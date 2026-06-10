// @vitest-environment jsdom

import { createElement } from "react";
import { cleanup, render } from "@testing-library/react";
import { afterEach, describe, expect, it } from "vitest";
import { UserSessionAvatar } from "./UserSessionAvatar.js";

describe("UserSessionAvatar", () => {
  afterEach(() => {
    cleanup();
  });

  it("AC1: renders 3D user icon with gradient definitions", () => {
    const { container } = render(createElement(UserSessionAvatar));

    const svg = container.querySelector("svg");
    expect(svg).not.toBeNull();
    expect(container.querySelector("linearGradient")).not.toBeNull();
    expect(container.querySelector("circle[fill^='url']")).not.toBeNull();

    const shell = container.firstElementChild;
    expect(shell?.className).toContain("bg-gradient-to-b");
    expect(shell?.className).toContain("shadow-[");
  });

  it("AC3: is decorative and hidden from assistive technologies", () => {
    const { container } = render(createElement(UserSessionAvatar));

    expect(container.firstElementChild).toHaveAttribute("aria-hidden", "true");
  });
});
