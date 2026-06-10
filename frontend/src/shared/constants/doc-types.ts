export const DOC_TYPES = ["CC", "CE", "NIT", "PA", "TI"] as const;
export type DocType = (typeof DOC_TYPES)[number];

export const DOC_TYPE_OPTIONS = DOC_TYPES.map((v) => ({ label: v, value: v }));
