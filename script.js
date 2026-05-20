// THEME TOGGLE
(function () {
  const toggle = document.querySelector('[data-theme-toggle]');
  const html = document.documentElement;
  const sunIcon = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="5"/><path d="M12 1v2M12 21v2M4.22 4.22l1.42 1.42M18.36 18.36l1.42 1.42M1 12h2M21 12h2M4.22 19.78l1.42-1.42M18.36 5.64l1.42-1.42"/></svg>`;
  const moonIcon = `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z"/></svg>`;

  let theme = matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  html.setAttribute('data-theme', theme);
  if (toggle) toggle.innerHTML = theme === 'dark' ? sunIcon : moonIcon;

  if (toggle) {
    toggle.addEventListener('click', () => {
      theme = theme === 'dark' ? 'light' : 'dark';
      html.setAttribute('data-theme', theme);
      toggle.innerHTML = theme === 'dark' ? sunIcon : moonIcon;
      toggle.setAttribute('aria-label', 'Cambiar a modo ' + (theme === 'dark' ? 'claro' : 'oscuro'));
    });
  }
})();

// MOBILE NAV
(function () {
  const navToggle = document.getElementById('navToggle');
  const nav = document.querySelector('.nav');
  if (!navToggle || !nav) return;

  navToggle.addEventListener('click', () => {
    const open = nav.classList.toggle('open');
    navToggle.setAttribute('aria-expanded', open);
    navToggle.setAttribute('aria-label', open ? 'Cerrar menú' : 'Abrir menú');
  });

  nav.querySelectorAll('a').forEach(link => {
    link.addEventListener('click', () => {
      nav.classList.remove('open');
      navToggle.setAttribute('aria-expanded', 'false');
    });
  });
})();

// SCROLL REVEAL
(function () {
  const revealElements = document.querySelectorAll(
    '.skill-card, .project-card, .about-grid, .stats-row, .section-title'
  );
  if (!('IntersectionObserver' in window)) return;

  revealElements.forEach(el => {
    el.style.opacity = '0';
    el.style.transform = 'translateY(20px)';
    el.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
  });

  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          setTimeout(() => {
            entry.target.style.opacity = '1';
            entry.target.style.transform = 'translateY(0)';
          }, (entry.target.dataset.delay || 0));
          observer.unobserve(entry.target);
        }
      });
    },
    { threshold: 0.12, rootMargin: '0px 0px -40px 0px' }
  );

  revealElements.forEach((el, i) => {
    el.dataset.delay = (i % 4) * 80;
    observer.observe(el);
  });
})();

// CONTACT FORM
(function () {
  const form = document.getElementById('contactForm');
  const msg  = document.getElementById('formMsg');
  if (!form || !msg) return;

  form.addEventListener('submit', (e) => {
    e.preventDefault();
    const name    = form.name.value.trim();
    const email   = form.email.value.trim();
    const message = form.message.value.trim();

    if (!name || !email || !message) {
      msg.textContent = '⚠ Por favor completa todos los campos.';
      msg.style.color = 'var(--color-error)';
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
      msg.textContent = '⚠ Ingresa un correo válido.';
      msg.style.color = 'var(--color-error)';
      return;
    }

    const btn = form.querySelector('button[type="submit"]');
    btn.disabled = true;
    btn.textContent = 'Enviando...';

    setTimeout(() => {
      msg.textContent = '✅ ¡Mensaje enviado! Te responderé pronto.';
      msg.style.color = 'var(--color-success)';
      form.reset();
      btn.disabled = false;
      btn.textContent = 'Enviar mensaje';
    }, 1200);
  });
})();

// ANIMATE STATS
(function () {
  const stats = document.querySelectorAll('.stat-num');
  if (!stats.length || !('IntersectionObserver' in window)) return;

  const animateCount = (el) => {
    const target = parseInt(el.textContent);
    if (isNaN(target)) return;
    const suffix = el.textContent.replace(/[0-9]/g, '');
    let current = 0;
    const step = Math.ceil(target / 30);
    const timer = setInterval(() => {
      current = Math.min(current + step, target);
      el.textContent = current + suffix;
      if (current >= target) clearInterval(timer);
    }, 40);
  };

  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        animateCount(entry.target);
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.5 });

  stats.forEach(s => observer.observe(s));
})();
