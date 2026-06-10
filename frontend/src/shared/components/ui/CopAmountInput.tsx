import { useEffect, useRef, useState } from "react";
import { InputText } from "primereact/inputtext";
import { formatCopAmountInWords } from "../../lib/format-cop-amount-words.js";
import { parseCopDigitsFromInputValue } from "../../lib/parse-cop-input.js";

interface CopAmountInputFieldProps {
  id?: string;
  value: number | null;
  onChange: (next: number | null) => void;
  onLiveAmountChange?: (next: number | null) => void;
  className?: string;
  inputClassName?: string;
}

function formatCopDisplay(amount: number | null): string {
  if (amount === null || amount <= 0) {
    return "";
  }
  return amount.toLocaleString("es-CO");
}

/** Campo numérico COP; dispara onChange en cada tecla. */
export function CopAmountInputField({
  id,
  value,
  onChange,
  onLiveAmountChange,
  className = "",
  inputClassName = "",
}: CopAmountInputFieldProps) {
  const focusedRef = useRef(false);
  const [display, setDisplay] = useState(() => formatCopDisplay(value));

  useEffect(() => {
    if (!focusedRef.current) {
      setDisplay(formatCopDisplay(value));
    }
  }, [value]);

  function commitFromRaw(raw: string) {
    setDisplay(raw);
    const parsed = parseCopDigitsFromInputValue(raw);
    onLiveAmountChange?.(parsed);
    onChange(parsed);
  }

  return (
    <div className={className} data-testid="cop-currency-field">
      <InputText
        id={id}
        value={display}
        onChange={(e) => commitFromRaw(e.target.value)}
        onFocus={() => {
          focusedRef.current = true;
        }}
        onBlur={() => {
          focusedRef.current = false;
          const parsed = parseCopDigitsFromInputValue(display);
          setDisplay(formatCopDisplay(parsed));
        }}
        inputMode="numeric"
        placeholder="$ 0"
        className={`w-full flit-input-control ${inputClassName}`.trim()}
        aria-describedby={id ? `${id}-words` : undefined}
      />
    </div>
  );
}

interface CopAmountWordsRowProps {
  amount: number | null;
  id?: string;
}

export function CopAmountWordsRow({ amount, id }: CopAmountWordsRowProps) {
  const amountInWords = formatCopAmountInWords(amount);

  return (
    <p
      id={id ? `${id}-words` : undefined}
      className="flit-amount-words-row sm:col-span-2"
      aria-live="polite"
      aria-atomic="true"
      data-testid="cop-amount-words"
    >
      <span className="flit-amount-words-row__label">Valor en letras:</span>
      <span
        data-testid="cop-amount-words-value"
        className={
          amountInWords ? "flit-amount-words-row__value" : "flit-amount-words-row__placeholder"
        }
      >
        {amountInWords || "Ingrese el valor para ver la descripción."}
      </span>
    </p>
  );
}
