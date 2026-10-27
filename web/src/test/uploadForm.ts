import { File as NodeFile } from "node:buffer";
import { vi } from "vitest";

/**
 * jsdom's `FormData` and `File` are not the ones the Node `Request` of the fake network reads, so a form with a file would fail
 * there. The `FormData` the Node `Request` itself reads is the constructor of what its own `formData()` returns.
 */
const NodeFormData = (
  await new Response("a=1", {
    headers: { "content-type": "application/x-www-form-urlencoded" },
  }).formData()
).constructor as typeof FormData;

/** Makes the page under test build its upload form with the `FormData` the fake network reads; undone by `vi.unstubAllGlobals()`. */
export function useNodeFormData(): void {
  vi.stubGlobal("FormData", NodeFormData);
}

/** A file the Node `FormData` accepts. */
export function nodeFile(content: string, name: string, type: string): File {
  return new NodeFile([content], name, { type }) as unknown as File;
}

/** One part of a multipart request: its name, the file name when it is a file, and its text. */
export interface FormPart {
  name: string;
  fileName: string | null;
  text: string;
}

/**
 * The parts of a multipart request, read from its raw text, because the `formData()` of the Node `Request` does not parse
 * a form built in this environment.
 */
export async function formParts(request: Request): Promise<FormPart[]> {
  const raw = await request.clone().text();
  const part =
    /Content-Disposition: form-data; name="([^"]+)"(?:; filename="([^"]*)")?\r\n(?:Content-Type: [^\r\n]*\r\n)?\r\n([\s\S]*?)\r\n--/gi;
  return [...raw.matchAll(part)].map(([, name, fileName, text]) => ({
    name: name ?? "",
    fileName: fileName ?? null,
    text: text ?? "",
  }));
}

/** The text of the part with the name. */
export function partText(parts: readonly FormPart[], name: string): string {
  const found = parts.find((candidate) => candidate.name === name);
  if (!found) throw new Error(`the form has no part «${name}»`);
  return found.text;
}
