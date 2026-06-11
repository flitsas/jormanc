import { z } from "zod";

export const RoleSchema = z.object({
  id: z.string().uuid(),
  slug: z.string(),
  name: z.string(),
  description: z.string().optional(),
  is_system: z.boolean().default(false),
});

export const UserSchema = z.object({
  id: z.string().uuid(),
  email: z.string().email(),
  full_name: z.string(),
  status: z.enum(["pending", "active", "suspended", "deleted"]),
  must_reset_pwd: z.boolean().default(false),
  last_login_at: z.string().nullable().optional(),
  created_at: z.string(),
  roles: z.array(RoleSchema).default([]),
});

export const PaginatedUsersSchema = z.object({
  items: z.array(UserSchema),
  total: z.number(),
  page: z.number(),
  page_size: z.number(),
});

export const UpdateUserRolesResponseSchema = z.object({
  id: z.string().uuid(),
  roles: z.array(RoleSchema),
});

export const InviteUserFormSchema = z.object({
  email: z.string().email("Email inválido"),
  roles: z.array(z.string().uuid()).min(1, "Selecciona al menos un rol"),
});

export type Role = z.infer<typeof RoleSchema>;
export type User = z.infer<typeof UserSchema>;
export type PaginatedUsers = z.infer<typeof PaginatedUsersSchema>;
export type InviteUserFormValues = z.infer<typeof InviteUserFormSchema>;
