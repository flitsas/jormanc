import { z } from "zod";

export const UserProfileSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  email: z.string().email(),
  roles: z.array(z.string()),
  permissions: z.array(z.string()),
  tenant_id: z.string().uuid(),
  tenant_name: z.string(),
});

export const LoginResponseSchema = z.object({
  access_token: z.string(),
  expires_in: z.number(),
  user: UserProfileSchema,
});

export const InvitationValidateSchema = z.object({
  email: z.string().email(),
  tenant_name: z.string(),
  roles: z.array(z.string()),
});

export const AcceptInvitationResponseSchema = z.object({
  access_token: z.string(),
  user: UserProfileSchema,
});

const strongPassword = z
  .string()
  .min(8, "Mínimo 8 caracteres")
  .regex(/[A-Z]/, "Debe contener al menos una mayúscula")
  .regex(/[0-9]/, "Debe contener al menos un número");

export const LoginFormSchema = z.object({
  email: z.string().email("Email inválido"),
  password: z.string().min(1, "Contraseña requerida"),
  tenant_slug: z.string().min(1, "Organización requerida"),
});

export const AcceptInvitationFormSchema = z
  .object({
    full_name: z.string().min(1, "Nombre requerido"),
    password: strongPassword,
    password_confirm: z.string().min(1, "Confirmar contraseña"),
  })
  .refine((d) => d.password === d.password_confirm, {
    message: "Las contraseñas no coinciden",
    path: ["password_confirm"],
  });

export const ForgotPasswordFormSchema = z.object({
  email: z.string().email("Email inválido"),
});

export const ResetPasswordFormSchema = z
  .object({
    password: strongPassword,
    password_confirm: z.string().min(1, "Confirmar contraseña"),
  })
  .refine((d) => d.password === d.password_confirm, {
    message: "Las contraseñas no coinciden",
    path: ["password_confirm"],
  });

export type UserProfile = z.infer<typeof UserProfileSchema>;
export type LoginResponse = z.infer<typeof LoginResponseSchema>;
export type InvitationValidate = z.infer<typeof InvitationValidateSchema>;
export type LoginFormValues = z.infer<typeof LoginFormSchema>;
export type AcceptInvitationFormValues = z.infer<typeof AcceptInvitationFormSchema>;
export type ForgotPasswordFormValues = z.infer<typeof ForgotPasswordFormSchema>;
export type ResetPasswordFormValues = z.infer<typeof ResetPasswordFormSchema>;
