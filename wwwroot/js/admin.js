document.addEventListener("DOMContentLoaded", () => {
  const body = document.body;
  document.querySelector("[data-sidebar-open]")?.addEventListener("click", () => body.classList.add("sidebar-open"));
  document.querySelectorAll("[data-sidebar-close]").forEach(el => el.addEventListener("click", () => body.classList.remove("sidebar-open")));
  document.addEventListener("keydown", event => { if (event.key === "Escape") body.classList.remove("sidebar-open"); });
});
