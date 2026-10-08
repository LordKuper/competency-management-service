/*
 * ASD migration -> 9.0.0. Config schema revision: `documents.c4` folds into `project.diagram_tool`
 * (new value `none`), `skip_design_phases` folds into the design document flags, legacy
 * `documents.audit` values `enabled`/`disabled` become `always`/`off`, and eight keys with no
 * remaining reader are dropped. This rewrites `.asd/project/config.yaml` only - as a release
 * migration it is a sanctioned settings writer, limited to release-mandated key renames and
 * removals, plus the value mappings, key insertions and shipped-comment rewrites that carry a
 * renamed or removed key's or value's intent. Sprint state is never touched: an active sprint keeps running on the documents
 * snapshot frozen at its scope.
 *
 * Runs in consumer projects via `/asd-update`. Sole non-consumer use: the ASD framework repo's own
 * self-hosting sprint runs it once, as a plan-declared task, against its own config - `/asd-update`
 * refuses to run there. Manual run from a project root:
 *   node -e "require('./.asd/migrations/9.0.0.js')({ repoRoot: process.cwd() })"
 *
 * Contract (see .asd/skills/asd-update/update.js's own header comment): filename (minus .js) is
 * the target asd_version; module.exports = (ctx) => MigrationReport | Promise<MigrationReport>
 * with ctx.repoRoot = the project root; zero-dependency Node; idempotent - re-running an
 * already-applied migration is a no-op, never an error. This script's MigrationReport shape is
 * `{ status, changes, reason }`, status one of `migrated | unchanged | absent | skipped`.
 *
 * No YAML parser: the rewrite is line-based and keeps every byte it does not own - comments,
 * blank lines, inline comment columns, line endings, BOM. Every comment is the user's except the
 * shipped-comment rewrites bounded above, on or directly above `documents.audit` /
 * `project.diagram_tool`. Anything beyond plain block-style
 * `key: scalar` lines under consistent indentation (flow maps, lists, block scalars, anchors,
 * tags, duplicate keys, tab indent, mixed line endings) skips the whole file with a warning
 * rather than guessing.
 */
'use strict';

const fs = require('fs');
const path = require('path');

const CONFIG_PATH = ['.asd', 'project', 'config.yaml'];
const REMOVED_KEYS = [
  'documents.c4',
  'skip_design_phases',
  'git.gh_enabled',
  'git.auto_pr',
  'system.os',
  'system.tools.likec4',
  'system.tools.designmd',
  'review.scoped_fan_out',
];
const DESIGN_DOCUMENTS = ['prd', 'ux_spec', 'adr'];
const LEGACY_AUDIT = { enabled: 'always', disabled: 'off' };
const TOGGLE_VALUES = ['enabled', 'disabled'];
const ENUMERATED_KEYS = {
  'documents.c4': TOGGLE_VALUES,
  skip_design_phases: TOGGLE_VALUES,
  'documents.audit': ['auto', 'always', 'off', ...Object.keys(LEGACY_AUDIT)],
};
const DEFAULT_DIAGRAM_TOOL = 'likec4';
const DIAGRAM_NONE = 'none';
const DEFAULT_CHILD_INDENT = 2;

const SHIPPED_AUDIT_COMMENT = '# auto | always | off; legacy enabled/disabled accepted';
const CURRENT_AUDIT_COMMENT = '# auto | always | off';
const CURRENT_DIAGRAM_HEADER = '# Diagram tool for architecture views. Any diagram also requires subsystem_decomposition: enabled.';
const CURRENT_DIAGRAM_NONE = '# none — no diagram written; the subsystem registry is unaffected';
// Earlier releases' verbatim `project.diagram_tool` comment lines, mapped to the lines replacing them.
const SHIPPED_DIAGRAM_COMMENTS = new Map([
  ['# Diagram tool for the subsystem registry and architecture views. Only used when decomposition enabled.', [CURRENT_DIAGRAM_HEADER, CURRENT_DIAGRAM_NONE]],
  ['# Diagram tool for architecture views. Only used when decomposition and documents.c4 are enabled.', [CURRENT_DIAGRAM_HEADER, CURRENT_DIAGRAM_NONE]],
  ['# Values: likec4 | mermaid', ['# Values: none | likec4 | mermaid']],
]);

const KEY_LINE = /^( *)([A-Za-z_][A-Za-z0-9_-]*):(?: +(.*))?$/;
const UNSUPPORTED_VALUE_START = /^[{[|>&*!]/;
const DOUBLE_QUOTED = /^("(?:[^"\\]|\\.)*")(\s+#.*|\s*)$/;
const SINGLE_QUOTED = /^('(?:[^']|'')*')(\s+#.*|\s*)$/;
const PLAIN = /^(.*?)(\s+#.*)?\s*$/;

const isBlank = (line) => line.trim() === '';
const isComment = (line) => /^ *#/.test(line);
const indentOf = (line) => line.length - line.trimStart().length;

// Null value = group header (no scalar after the colon); undefined result = unsupported shape.
function parseScalar(rest) {
  if (rest === '' || rest.startsWith('#')) return { value: null, length: 0 };
  if (UNSUPPORTED_VALUE_START.test(rest)) return undefined;
  const quoted = DOUBLE_QUOTED.exec(rest) || SINGLE_QUOTED.exec(rest);
  if (quoted) return { value: quoted[1].slice(1, -1), length: quoted[1].length };
  if (rest.startsWith('"') || rest.startsWith("'")) return undefined;
  const plain = PLAIN.exec(rest)[1];
  return { value: plain, length: plain.length };
}

function isHeaderLine(line) {
  const match = line === undefined ? null : KEY_LINE.exec(line);
  return Boolean(match) && parseScalar(match[3] || '').value === null;
}

// Maps each dotted key path to its line: index, indent, scalar value and span, the last line of
// its subtree and the indent its children use. `reason` instead when any line is unsupported.
function parseEntries(lines) {
  const entries = new Map();
  const stack = [];
  for (let index = 0; index < lines.length; index++) {
    const line = lines[index];
    if (isBlank(line) || isComment(line)) continue;
    const match = KEY_LINE.exec(line);
    if (!match) return { reason: `line ${index + 1} is not a plain block-style \`key: value\` line` };
    const indent = match[1].length;
    while (stack.length > 0 && stack[stack.length - 1].indent >= indent) stack.pop();
    const parent = stack[stack.length - 1];
    if (indent > 0 && !parent) return { reason: `line ${index + 1} is indented under no group` };
    if (parent && parent.childIndent !== undefined && parent.childIndent !== indent) {
      return { reason: `line ${index + 1} breaks its group's indentation` };
    }
    const keyPath = parent ? `${parent.path}.${match[2]}` : match[2];
    if (entries.has(keyPath)) return { reason: `line ${index + 1} duplicates key \`${keyPath}\`` };
    const rest = match[3] || '';
    const scalar = parseScalar(rest);
    if (!scalar) return { reason: `line ${index + 1} holds a value shape this migration does not rewrite` };
    if (parent) parent.childIndent = indent;
    const entry = {
      path: keyPath, index, indent, parent, lastIndex: index, childIndent: undefined,
      value: scalar.value, valueStart: line.length - rest.length, valueLength: scalar.length,
    };
    entries.set(keyPath, entry);
    for (let ancestor = parent; ancestor; ancestor = ancestor.parent) ancestor.lastIndex = index;
    if (scalar.value === null) stack.push(entry);
  }
  return { entries };
}

// Leaves and groups this migration reads must have the shape and values it knows how to map.
function shapeProblem(entries) {
  const scalarKeys = [...REMOVED_KEYS, 'documents.audit', 'project.diagram_tool', ...DESIGN_DOCUMENTS.map((name) => `documents.${name}`)];
  for (const key of scalarKeys) {
    const entry = entries.get(key);
    const allowed = ENUMERATED_KEYS[key];
    if (!entry) continue;
    if (entry.value === null) return `\`${key}\` is a group, expected a scalar`;
    if (allowed && !allowed.includes(entry.value)) return `\`${key}: ${entry.value}\` is not one of ${allowed.join(' | ')}`;
  }
  for (const group of ['documents', 'project']) {
    const entry = entries.get(group);
    if (entry && entry.value !== null) return `\`${group}\` is a scalar, expected a group`;
  }
  return null;
}

// The diagram tool the old `documents.c4`/`skip_design_phases` pair meant, or undefined when the
// config carries no pre-9.0.0 key (already migrated - an absent `c4` then means nothing).
function targetDiagramTool(entries) {
  if (!REMOVED_KEYS.some((key) => entries.has(key))) return undefined;
  const valueOf = (key) => (entries.has(key) ? entries.get(key).value : undefined);
  if (valueOf('skip_design_phases') === 'enabled') return DIAGRAM_NONE;
  const c4 = valueOf('documents.c4') || (entries.has('documents') ? 'disabled' : 'enabled');
  if (c4 === 'disabled') return DIAGRAM_NONE;
  return valueOf('project.diagram_tool') === undefined ? DEFAULT_DIAGRAM_TOOL : valueOf('project.diagram_tool');
}

// Replaces a scalar in place, keeping an inline comment on its original column where it fits.
function setScalar(lines, entry, value, changes) {
  if (entry.value === value) return;
  const line = lines[entry.index];
  const valueEnd = entry.valueStart + entry.valueLength;
  const tail = line.slice(valueEnd);
  const gap = /^ +(?=#)/.exec(tail);
  const spaces = gap ? ' '.repeat(Math.max(1, gap[0].length + entry.valueLength - value.length)) : '';
  lines[entry.index] = line.slice(0, entry.valueStart) + value + (gap ? spaces + tail.slice(gap[0].length) : tail);
  changes.push(`${entry.path}: ${entry.value} -> ${value}`);
}

function appendGroup(lines, group, leaves, changes) {
  if (lines.length > 0 && !isBlank(lines[lines.length - 1])) lines.push('');
  lines.push(`${group}:`);
  for (const [key, value] of leaves) {
    lines.push(`${' '.repeat(DEFAULT_CHILD_INDENT)}${key}: ${value}`);
    changes.push(`${group}.${key}: (absent) -> ${value}`);
  }
}

// Value rewrites and key insertions - every edit that keeps existing line indices valid, or
// appends past them.
function applyValueChanges(lines, entries, changes) {
  const audit = entries.get('documents.audit');
  if (audit && LEGACY_AUDIT[audit.value]) setScalar(lines, audit, LEGACY_AUDIT[audit.value], changes);
  const skipDesign = entries.has('skip_design_phases') && entries.get('skip_design_phases').value === 'enabled';
  if (skipDesign) {
    for (const name of DESIGN_DOCUMENTS) {
      const entry = entries.get(`documents.${name}`);
      if (entry) setScalar(lines, entry, 'disabled', changes);
    }
  }
  const diagramTool = targetDiagramTool(entries);
  const project = entries.get('project');
  const current = entries.get('project.diagram_tool');
  if (diagramTool !== undefined && current) setScalar(lines, current, diagramTool, changes);
  if (diagramTool !== undefined && !current && project) {
    const indent = ' '.repeat(project.childIndent === undefined ? DEFAULT_CHILD_INDENT : project.childIndent);
    lines.splice(project.lastIndex + 1, 0, `${indent}diagram_tool: ${diagramTool}`);
    changes.push(`project.diagram_tool: (absent) -> ${diagramTool}`);
  }
  if (skipDesign && !entries.has('documents')) {
    appendGroup(lines, 'documents', [['audit', LEGACY_AUDIT.enabled], ...DESIGN_DOCUMENTS.map((name) => [name, 'disabled'])], changes);
  }
  if (diagramTool !== undefined && !current && !project) appendGroup(lines, 'project', [['diagram_tool', diagramTool]], changes);
}

// First line of the comment block a removed key owns: directly above it at its indent, opened by
// a blank line, a group header or the file start, and closed by the key ending its paragraph.
// Anything looser may be a shared comment, so the key goes alone.
function ownedBlockStart(lines, entry) {
  const { start } = commentBlockAbove(lines, entry);
  const above = lines[start - 1];
  const below = lines[entry.index + 1];
  const opensParagraph = start === 0 || isBlank(above) || isHeaderLine(above);
  const endsParagraph = below === undefined || isBlank(below) || indentOf(below) < entry.indent;
  return opensParagraph && endsParagraph ? start : entry.index;
}

// Removes a key with its owned comments, then collapses the blank line the removal left doubled
// or leading its still non-empty group.
function removeKey(lines, entry, changes) {
  const start = ownedBlockStart(lines, entry);
  lines.splice(start, entry.index - start + 1);
  const above = lines[start - 1];
  const below = lines[start];
  const after = lines[start + 1];
  const leadsGroup = start === 0 || (isHeaderLine(above) && after !== undefined && indentOf(after) > indentOf(above));
  if (start > 0 && isBlank(above) && (below === undefined || isBlank(below))) lines.splice(start - 1, 1);
  else if (leadsGroup && below !== undefined && isBlank(below)) lines.splice(start, 1);
  changes.push(`${entry.path}: removed`);
}

// Comment lines above a key, contiguous and at its indent.
function commentBlockAbove(lines, entry) {
  let start = entry.index;
  while (start > 0 && isComment(lines[start - 1]) && indentOf(lines[start - 1]) === entry.indent) start--;
  return { start, block: lines.slice(start, entry.index) };
}

// Replaces template-shipped comments whose contract this release changed; user-written ones never match.
function rewriteShippedComments(lines, entries, changes) {
  const audit = entries.get('documents.audit');
  if (audit && lines[audit.index].endsWith(SHIPPED_AUDIT_COMMENT)) {
    lines[audit.index] = lines[audit.index].slice(0, -SHIPPED_AUDIT_COMMENT.length) + CURRENT_AUDIT_COMMENT;
    changes.push('documents.audit: shipped comment -> current wording');
  }
  const diagramTool = entries.get('project.diagram_tool');
  if (!diagramTool) return;
  const { start, block } = commentBlockAbove(lines, diagramTool);
  const hasNoneLine = block.some((line) => line.trim() === CURRENT_DIAGRAM_NONE);
  const rewritten = block.flatMap((line) => {
    const replacement = SHIPPED_DIAGRAM_COMMENTS.get(line.trim());
    if (!replacement) return [line];
    const indent = ' '.repeat(indentOf(line));
    return replacement.filter((text) => !(hasNoneLine && text === CURRENT_DIAGRAM_NONE)).map((text) => indent + text);
  });
  if (rewritten.join('\n') === block.join('\n')) return;
  lines.splice(start, block.length, ...rewritten);
  changes.push('project.diagram_tool: shipped comments -> current wording');
}

function reparse(lines) {
  const parsed = parseEntries(lines);
  if (parsed.reason) throw new Error(`9.0.0 produced an unparsable config (${parsed.reason}) - no file written`);
  return parsed.entries;
}

// LF-joined text in, LF-joined text out; a trailing newline is kept apart so appends land before it.
function migrateText(text) {
  const hasTrailingNewline = text.endsWith('\n');
  const lines = (hasTrailingNewline ? text.slice(0, -1) : text).split('\n');
  const parsed = parseEntries(lines);
  if (parsed.reason) return { reason: parsed.reason };
  const problem = shapeProblem(parsed.entries);
  if (problem) return { reason: problem };
  const changes = [];
  applyValueChanges(lines, parsed.entries, changes);
  const afterValues = reparse(lines);
  const removed = REMOVED_KEYS.map((key) => afterValues.get(key)).filter(Boolean).sort((a, b) => b.index - a.index);
  for (const entry of removed) removeKey(lines, entry, changes);
  rewriteShippedComments(lines, reparse(lines), changes);
  return { text: lines.join('\n') + (hasTrailingNewline ? '\n' : ''), changes };
}

// Temp file plus rename: an interrupted run leaves the original config intact rather than truncated.
function replaceFileAtomically(filePath, text) {
  const tempPath = `${filePath}.asd-migration.tmp`;
  fs.writeFileSync(tempPath, text, { encoding: 'utf8' });
  fs.renameSync(tempPath, filePath);
}

/** Rewrites `.asd/project/config.yaml` to the 9.0.0 schema; the report lists each key change, or why the file was skipped. */
module.exports = function migrate(ctx) {
  const rel = CONFIG_PATH.join('/');
  const configPath = path.join(ctx.repoRoot, ...CONFIG_PATH);
  if (!fs.existsSync(configPath)) return { status: 'absent', changes: [], reason: null };
  const raw = fs.readFileSync(configPath, 'utf8');
  const bom = raw.startsWith('﻿') ? '﻿' : '';
  const body = raw.slice(bom.length);
  const eol = body.includes('\r\n') ? '\r\n' : '\n';
  const skip = (reason) => {
    process.stdout.write(
      `asd-migration 9.0.0: warning: ${rel} left untouched - ${reason}. Fix that line, then re-run from ` +
      `the project root: node -e "require('./.asd/migrations/9.0.0.js')({ repoRoot: process.cwd() })"\n`
    );
    return { status: 'skipped', changes: [], reason };
  };
  if (eol === '\r\n' && /(^|[^\r])\n/.test(body)) return skip('it mixes CRLF and LF line endings');
  if (/\r(?!\n)/.test(body)) return skip('it holds a bare CR line ending');
  const result = migrateText(body.split(eol).join('\n'));
  if (result.reason) return skip(result.reason);
  const next = bom + result.text.split('\n').join(eol);
  if (next === raw) return { status: 'unchanged', changes: [], reason: null };
  replaceFileAtomically(configPath, next);
  return { status: 'migrated', changes: result.changes, reason: null };
};
