const UNITS = [
  "cero",
  "uno",
  "dos",
  "tres",
  "cuatro",
  "cinco",
  "seis",
  "siete",
  "ocho",
  "nueve",
] as const;

const TEENS = [
  "diez",
  "once",
  "doce",
  "trece",
  "catorce",
  "quince",
  "dieciséis",
  "diecisiete",
  "dieciocho",
  "diecinueve",
] as const;

const TENS = [
  "",
  "",
  "veinte",
  "treinta",
  "cuarenta",
  "cincuenta",
  "sesenta",
  "setenta",
  "ochenta",
  "noventa",
] as const;

const HUNDREDS = [
  "",
  "ciento",
  "doscientos",
  "trescientos",
  "cuatrocientos",
  "quinientos",
  "seiscientos",
  "setecientos",
  "ochocientos",
  "novecientos",
] as const;

function convertLessThan100(n: number): string {
  if (n === 0) return "";
  if (n < 10) return UNITS[n] ?? "";
  if (n < 20) return TEENS[n - 10] ?? "";
  const ten = Math.floor(n / 10);
  const unit = n % 10;
  if (unit === 0) return TENS[ten] ?? "";
  if (ten === 2) {
    const veinti = [
      "veinte",
      "veintiuno",
      "veintidós",
      "veintitrés",
      "veinticuatro",
      "veinticinco",
      "veintiséis",
      "veintisiete",
      "veintiocho",
      "veintinueve",
    ];
    return veinti[unit] ?? "";
  }
  return `${TENS[ten]} y ${UNITS[unit]}`;
}

function convertLessThan1000(n: number): string {
  if (n === 0) return "";
  if (n === 100) return "cien";
  const hundred = Math.floor(n / 100);
  const rest = n % 100;
  const hundredPart = hundred > 0 ? HUNDREDS[hundred] : "";
  const restPart = convertLessThan100(rest);
  if (!hundredPart) return restPart;
  if (!restPart) return hundredPart;
  return `${hundredPart} ${restPart}`;
}

function convertGroup(n: number, singular: string, plural: string): string {
  if (n === 0) return "";
  if (n === 1) return singular;
  return `${convertLessThan1000(n)} ${plural}`;
}

function convertInteger(n: number): string {
  if (n === 0) return "cero";

  const parts: string[] = [];

  const billions = Math.floor(n / 1_000_000_000);
  const millions = Math.floor((n % 1_000_000_000) / 1_000_000);
  const thousands = Math.floor((n % 1_000_000) / 1_000);
  const rest = n % 1_000;

  const billionPart = convertGroup(billions, "mil millones", "mil millones");
  if (billionPart) parts.push(billionPart);

  if (millions > 0) {
    parts.push(millions === 1 ? "un millón" : `${convertLessThan1000(millions)} millones`);
  }

  if (thousands > 0) {
    parts.push(thousands === 1 ? "mil" : `${convertLessThan1000(thousands)} mil`);
  }

  if (rest > 0) {
    parts.push(convertLessThan1000(rest));
  }

  return parts.join(" ").replace(/\s+/g, " ").trim();
}

function capitalizeFirst(text: string): string {
  if (!text) return "";
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/**
 * Convierte un valor entero en pesos COP a texto (es-CO), solo visual.
 * Ej: 50000000 → "Cincuenta millones de pesos"
 */
export function formatCopAmountInWords(amount: number | null | undefined): string {
  if (amount === null || amount === undefined || !Number.isFinite(amount) || amount <= 0) {
    return "";
  }

  const pesos = Math.floor(amount);
  if (pesos === 0) return "";

  let words = convertInteger(pesos);
  words = words.replace(/^uno /, "un ").replace(/\suno /g, " un ");
  const pesoLabel = pesos === 1 ? "peso" : "pesos";
  return capitalizeFirst(`${words} ${pesoLabel}`);
}
