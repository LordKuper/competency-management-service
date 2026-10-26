"""Builds an org-structure import workbook (.xlsx) from the working employees of a seeded database, for measuring the import preview and apply.

Reads CSV rows from stdin and writes the workbook to the path given as the only argument. Each row has the unit names of levels 1 to 7
(empty where the path is shorter), the full name, the position and the legal entity. Produce the rows from a database seeded with
tests/perf/seed-10k.sql, for example through the dev database container:

    docker exec <db-container> psql -U <user> -d <database> -c "COPY (<query below>) TO STDOUT WITH CSV"

    WITH RECURSIVE path(id, names) AS (
        SELECT id, ARRAY[name::text] FROM org_units WHERE parent_id IS NULL
        UNION ALL SELECT u.id, p.names || u.name::text FROM org_units u JOIN path p ON u.parent_id = p.id)
    SELECT p.names[1], p.names[2], p.names[3], p.names[4], p.names[5], p.names[6], p.names[7],
           e.last_name || ' ' || e.first_name || coalesce(' ' || e.middle_name, ''), pos.name,
           'Юрлицо ' || (row_number() OVER (ORDER BY e.id) % 3)
    FROM employees e JOIN path p ON p.id = e.org_unit_id JOIN positions pos ON pos.id = e.position_id
    WHERE e.is_active ORDER BY e.id

Run it as: python -I build-org-import-xlsx.py out.xlsx < rows.csv (standard library only).
"""
import csv
import sys
import zipfile
from xml.sax.saxutils import escape

LEVELS = 7
HEADER = [f"Подразделение {level}-го уровня" for level in range(1, LEVELS + 1)] + ["ФИО", "Должность", "Юридическое лицо"]


def column_letter(index):
    letters = ""
    index += 1
    while index:
        index, rest = divmod(index - 1, 26)
        letters = chr(65 + rest) + letters
    return letters


def row_xml(number, values):
    cells = "".join(
        f'<c r="{column_letter(index)}{number}" t="inlineStr"><is><t>{escape(value)}</t></is></c>'
        for index, value in enumerate(values)
        if value != ""
    )
    return f'<row r="{number}">{cells}</row>'


def main(path):
    rows = [HEADER] + list(csv.reader(sys.stdin, delimiter=","))
    sheet = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>'
        + "".join(row_xml(number, values) for number, values in enumerate(rows, start=1))
        + "</sheetData></worksheet>"
    )
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        archive.writestr(
            "[Content_Types].xml",
            '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
            '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
            '<Default Extension="xml" ContentType="application/xml"/>'
            '<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>'
            '<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>'
            "</Types>",
        )
        archive.writestr(
            "_rels/.rels",
            '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>'
            "</Relationships>",
        )
        archive.writestr(
            "xl/workbook.xml",
            '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" '
            'xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">'
            '<sheets><sheet name="Лист1" sheetId="1" r:id="rId1"/></sheets></workbook>',
        )
        archive.writestr(
            "xl/_rels/workbook.xml.rels",
            '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>'
            "</Relationships>",
        )
        archive.writestr("xl/worksheets/sheet1.xml", sheet)
    print(f"{len(rows) - 1} rows written to {path}")


if __name__ == "__main__":
    sys.stdin.reconfigure(encoding="utf-8")
    sys.stdout.reconfigure(encoding="utf-8")
    main(sys.argv[1])
