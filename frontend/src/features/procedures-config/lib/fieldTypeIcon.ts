export function fieldTypeIcon(fieldType: string): string {
  switch (fieldType) {
    case "dropdown":
      return "pi-list";
    case "checkbox":
      return "pi-check-square";
    case "numeric":
      return "pi-hashtag";
    case "attachment":
      return "pi-paperclip";
    case "list":
      return "pi-table";
    default:
      return "pi-align-left";
  }
}

export function fieldTypeLabel(fieldType: string): string {
  const labels: Record<string, string> = {
    text: "Texto",
    dropdown: "Lista desplegable",
    checkbox: "Casilla",
    numeric: "Numérico",
    attachment: "Adjunto",
    list: "Lista",
  };
  return labels[fieldType] ?? fieldType;
}
