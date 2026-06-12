import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { apiClient, TOKEN_KEY, USER_KEY } from "../../../shared/api/client.js";
import {
  LoginResponseSchema,
  UserProfileSchema,
  InvitationValidateSchema,
  AcceptInvitationResponseSchema,
  type UserProfile,
  type LoginFormValues,
  type AcceptInvitationFormValues,
  type ForgotPasswordFormValues,
  type ResetPasswordFormValues,
} from "./auth.schemas.js";

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function getStoredUser(): UserProfile | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    return UserProfileSchema.parse(JSON.parse(raw));
  } catch {
    return null;
  }
}

export function setAuth(token: string, user: UserProfile): void {
  localStorage.setItem(TOKEN_KEY, token);
  localStorage.setItem(USER_KEY, JSON.stringify(user));
}

export function clearAuth(): void {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
}

export const authQueryKeys = {
  me: ["auth", "me"] as const,
  invitation: (token: string) => ["auth", "invitation", token] as const,
};

export function useLogin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: LoginFormValues) => {
      const res = await apiClient.post("/auth/login", data);
      return LoginResponseSchema.parse(res.data);
    },
    onSuccess: (data) => {
      setAuth(data.accessToken, data.user);
      queryClient.setQueryData(authQueryKeys.me, data.user);
    },
  });
}

export function useMe() {
  return useQuery({
    queryKey: authQueryKeys.me,
    queryFn: async () => {
      const res = await apiClient.get("/auth/me");
      return UserProfileSchema.parse(res.data);
    },
    enabled: !!getStoredToken(),
    staleTime: 5 * 60 * 1000,
    retry: false,
  });
}

export function useValidateInvitation(token: string) {
  return useQuery({
    queryKey: authQueryKeys.invitation(token),
    queryFn: async () => {
      const res = await apiClient.get(`/invitations/${token}/validate`);
      return InvitationValidateSchema.parse(res.data);
    },
    enabled: !!token,
    retry: false,
  });
}

export function useAcceptInvitation(token: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: AcceptInvitationFormValues) => {
      const res = await apiClient.post(`/invitations/${token}/accept`, {
        fullName: data.full_name,
        password: data.password,
      });
      return AcceptInvitationResponseSchema.parse(res.data);
    },
    onSuccess: (data) => {
      setAuth(data.accessToken, data.user);
      queryClient.setQueryData(authQueryKeys.me, data.user);
    },
  });
}

export function useForgotPassword() {
  return useMutation({
    mutationFn: async (data: ForgotPasswordFormValues) => {
      await apiClient.post("/auth/forgot-password", data);
    },
  });
}

export function useResetPassword(token: string) {
  return useMutation({
    mutationFn: async (data: ResetPasswordFormValues) => {
      await apiClient.post("/auth/reset-password", {
        token,
        newPassword: data.password,
      });
    },
  });
}

export function useLogout() {
  const queryClient = useQueryClient();

  return () => {
    clearAuth();
    queryClient.clear();
    window.location.href = "/login";
  };
}
