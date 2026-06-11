import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "../../../shared/api/client.js";
import {
  PaginatedUsersSchema,
  RoleSchema,
  UpdateUserRolesResponseSchema,
  type InviteUserFormValues,
} from "./users.schemas.js";
import { z } from "zod";

export const usersQueryKeys = {
  all: ["users"] as const,
  list: (page: number, pageSize: number, search?: string) =>
    ["users", "list", page, pageSize, search ?? ""] as const,
  roles: ["roles"] as const,
};

export function useUsers(page = 1, pageSize = 20, search?: string) {
  return useQuery({
    queryKey: usersQueryKeys.list(page, pageSize, search),
    queryFn: async () => {
      const params = new URLSearchParams({
        page: String(page),
        page_size: String(pageSize),
      });
      if (search) params.set("search", search);

      const res = await apiClient.get(`/users?${params.toString()}`);
      return PaginatedUsersSchema.parse(res.data);
    },
    staleTime: 30_000,
  });
}

export function useRoles() {
  return useQuery({
    queryKey: usersQueryKeys.roles,
    queryFn: async () => {
      const res = await apiClient.get("/roles");
      return z.array(RoleSchema).parse(res.data);
    },
    staleTime: 5 * 60 * 1000,
  });
}

export function useUpdateUserRoles(userId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (roleIds: string[]) => {
      const res = await apiClient.patch(`/users/${userId}/roles`, { roles: roleIds });
      return UpdateUserRolesResponseSchema.parse(res.data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usersQueryKeys.all });
    },
  });
}

export function useCreateInvitation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: InviteUserFormValues) => {
      const res = await apiClient.post("/invitations", {
        email: data.email,
        roles: data.roles,
      });
      return res.data as { id: string; email: string; expires_at: string };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: usersQueryKeys.all });
    },
  });
}
