(() => {
  const body = document.body;
  if (!body.classList.contains("app-shell")) return;

  const sidebar = document.getElementById("appSidebar");
  const backdrop = document.getElementById("sidebarBackdrop");
  const openBtn = document.getElementById("menuToggle");
  const closeBtn = document.getElementById("sidebarClose");

  if (!sidebar || !openBtn) return;

  const setOpen = (open) => {
    body.classList.toggle("nav-open", open);
    openBtn.setAttribute("aria-expanded", open ? "true" : "false");
    openBtn.setAttribute("aria-label", open ? "Close menu" : "Open menu");
    if (backdrop) backdrop.hidden = !open;
    body.style.overflow = open && window.matchMedia("(max-width: 900px)").matches ? "hidden" : "";
  };

  openBtn.addEventListener("click", () => setOpen(!body.classList.contains("nav-open")));
  closeBtn?.addEventListener("click", () => setOpen(false));
  backdrop?.addEventListener("click", () => setOpen(false));

  sidebar.querySelectorAll("a").forEach((link) => {
    link.addEventListener("click", () => {
      if (window.matchMedia("(max-width: 900px)").matches) setOpen(false);
    });
  });

  window.addEventListener("keydown", (e) => {
    if (e.key === "Escape") setOpen(false);
  });

  window.addEventListener("resize", () => {
    if (!window.matchMedia("(max-width: 900px)").matches) setOpen(false);
  });
})();
