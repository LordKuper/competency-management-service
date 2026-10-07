/** The text with the first occurrence of the lower-cased needle marked; the text as it is when the needle is blank or absent. */
export function Highlighted({
  text,
  needle,
}: {
  text: string;
  needle: string;
}) {
  const start = needle ? text.toLocaleLowerCase("ru").indexOf(needle) : -1;
  if (start < 0) return text;
  const end = start + needle.length;
  return (
    <>
      {text.slice(0, start)}
      <mark>{text.slice(start, end)}</mark>
      {text.slice(end)}
    </>
  );
}
