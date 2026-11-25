import type { Directive } from 'vue'

// One-time fade-up when the element scrolls into view (skipped for reduced motion).
// Styles (.reveal / .reveal-in) live in assets/main.css.
export const vReveal: Directive<HTMLElement> = {
  mounted(el) {
    // A component whose root is currently a v-if comment node has nothing to animate
    if (!(el instanceof HTMLElement)) return
    if (
      typeof IntersectionObserver === 'undefined' ||
      window.matchMedia('(prefers-reduced-motion: reduce)').matches
    ) {
      return
    }
    el.classList.add('reveal')
    const observer = new IntersectionObserver(
      (entries) => {
        if (!entries[0]?.isIntersecting) return
        el.classList.remove('reveal')
        el.classList.add('reveal-in')
        observer.disconnect()
      },
      { threshold: 0.08, rootMargin: '0px 0px -40px 0px' },
    )
    observer.observe(el)
  },
}
