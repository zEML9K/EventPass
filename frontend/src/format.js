// SQLite повертає дати без "Z" — вважаємо їх UTC, щоб час не "плавав".
export function formatDate(value) {
  const hasZone = /[zZ]$|[+-]\d\d:\d\d$/.test(value);
  const date = new Date(hasZone ? value : `${value}Z`);
  return date.toLocaleString("uk-UA", { dateStyle: "long", timeStyle: "short" });
}
