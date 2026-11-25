<template>
  <header
    class="sticky top-0 z-50 w-full border-b border-[#E8D9C9]/80 bg-[#FFF9F2]/85 shadow-sm shadow-[#332820]/5 backdrop-blur-xl"
  >
    <!-- brand accent line -->
    <div class="relative h-[3px] w-full bg-[#F2A65A]/25" role="progressbar" aria-label="ความคืบหน้าการเลื่อนหน้า" :aria-valuenow="Math.round(scrollProgress)" aria-valuemin="0" aria-valuemax="100">
      <div
        class="absolute inset-y-0 left-0 bg-gradient-to-r from-[#D96C2C] via-[#F2A65A] to-[#D96C2C]"
        :style="{ width: `${scrollProgress}%` }"
      ></div>
    </div>
    <nav
      aria-label="เมนูหลัก"
      class="mx-auto flex h-[3.75rem] w-full max-w-[1800px] items-center justify-between gap-3 px-4 sm:h-[4.75rem] sm:px-6 lg:px-10"
    >
      <!-- Logo Brand -->
      <RouterLink to="/" class="group flex min-w-0 shrink-0 items-center gap-2.5 sm:gap-3" @click="closeMenu">
        <div
          class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-[#D96C2C] via-[#BF5720] to-[#171412] text-white shadow-md shadow-[#D96C2C]/30 ring-2 ring-white transition-all duration-300 group-hover:-rotate-3 group-hover:scale-105"
        >
          <!-- Pagoda / Sacred Thai Emblem Icon -->
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-6 w-6 fill-current text-[#F2A65A]"
            viewBox="0 0 24 24"
          >
            <path
              d="M12 2L9.5 7H14.5L12 2ZM12 7.5L8.5 13H15.5L12 7.5ZM12 13.5L7 20H17L12 13.5ZM5 21H19V22H5V21Z"
            />
          </svg>
        </div>

        <div class="flex min-w-0 flex-col">
          <!-- Platform name: truncates where space is tight; full text is in the title tooltip -->
          <span
            class="max-w-[11rem] truncate text-sm leading-snug font-black tracking-tight text-[#332820] sm:max-w-[18rem] sm:text-base lg:max-w-[16rem] xl:max-w-[24rem] xl:text-lg"
            title="แพลตฟอร์มดิจิทัลเพื่อยกระดับคอนเทนต์ท้องถิ่นและเศรษฐกิจสร้างสรรค์ จังหวัดกาญจนบุรี"
          >
            แพลตฟอร์มดิจิทัลเพื่อยกระดับคอนเทนต์ท้องถิ่น
          </span>
          <span
            class="max-w-[11rem] truncate text-[11px] leading-snug font-bold tracking-wide text-[#D96C2C] sm:max-w-[18rem] sm:text-xs lg:max-w-[16rem] xl:max-w-[24rem]"
          >
            และเศรษฐกิจสร้างสรรค์ จังหวัดกาญจนบุรี
          </span>
        </div>
      </RouterLink>

      <!-- Desktop Menu Nav Links -->
      <ul
        class="hidden min-w-0 items-center justify-center gap-1 rounded-full border border-[#E8D9C9] bg-[#F7F0E6]/80 p-1 lg:flex"
      >
        <li v-for="item in navigationItems" :key="item.label">
          <RouterLink
            :to="item.to"
            class="nav-link"
            active-class="is-active"
            :exact-active-class="item.to === '/' ? 'is-active' : undefined"
          >
            <i :class="`mdi ${item.icon}`" class="text-lg"></i>
            {{ item.label }}
          </RouterLink>
        </li>
      </ul>

      <!-- Desktop Right Actions -->
      <div class="hidden shrink-0 items-center gap-3 lg:flex lg:gap-4">
        <!-- Search Quick Icon -->
        <button
          type="button"
          class="flex h-10 w-10 items-center justify-center rounded-full border border-[#E8D9C9] bg-white text-[#332820] transition hover:border-[#D96C2C] hover:bg-[#FFF1E8] cursor-pointer focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          title="ค้นหา"
          aria-label="ค้นหา"
          @click="toggleSearch"
        >
          <i class="mdi mdi-magnify text-2xl text-[#D96C2C]"></i>
        </button>

        <!-- Cart Button with Count Badge -->
        <RouterLink
          to="/cart"
          class="relative flex h-10 w-10 items-center justify-center rounded-full border border-[#E8D9C9] bg-white text-[#332820] transition hover:border-[#D96C2C] hover:bg-[#FFF1E8] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          title="ตะกร้าสินค้า"
          aria-label="ตะกร้าสินค้า"
        >
          <i class="mdi mdi-shopping-outline text-2xl text-[#D96C2C]"></i>
          <span
            v-if="auth.isLoggedIn && cart.itemCount"
            class="absolute -top-1 -right-1 flex h-5 min-w-5 items-center justify-center rounded-full border-2 border-white bg-[#D96C2C] px-1 text-[10px] font-black text-white shadow-sm"
          >
            {{ auth.isLoggedIn ? cart.itemCount : 0 }}
          </span>
        </RouterLink>

        <!-- Create Content CTA Button -->
        <RouterLink
          to="/create"
          class="flex items-center gap-1.5 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-4 py-2.5 text-xs font-black text-white shadow-md shadow-[#D96C2C]/30 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-[#D96C2C]/40 active:scale-95 cursor-pointer focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none sm:text-sm"
          title="ลงคอนเทนต์ท่องเที่ยวและวัฒนธรรมกาญจนบุรี"
        >
          <i class="mdi mdi-plus-circle text-base text-white"></i>
          <span class="font-black text-white">ลงคอนเทนต์</span>
        </RouterLink>

        <!-- Profile / Login Button -->
        <RouterLink v-if="!auth.isLoggedIn" to="/login" class="login-button gap-2">
          <i class="mdi mdi-account-circle-outline text-xl text-[#D96C2C]"></i>
          <span>เข้าสู่ระบบ</span>
        </RouterLink>

        <div v-else class="flex items-center gap-2">
          <RouterLink to="/profile" class="login-button gap-2">
            <i class="mdi mdi-account-circle-outline text-xl text-[#D96C2C]"></i>
            <span>{{ auth.user?.name || 'โปรไฟล์' }}</span>
          </RouterLink>
          <button
            class="flex h-10 w-10 items-center justify-center rounded-xl border-2 border-[#E8D9C9] bg-white text-[#786B62] hover:bg-rose-50 hover:text-rose-600 hover:border-rose-200 transition cursor-pointer"
            title="ออกจากระบบ"
            @click="logout"
          >
            <i class="mdi mdi-logout text-lg"></i>
          </button>
        </div>
      </div>

      <!-- Mobile / tablet: cart shortcut + hamburger -->
      <div class="flex items-center gap-2 lg:hidden">
      <RouterLink
        to="/cart"
        class="relative flex h-10 w-10 items-center justify-center rounded-xl border-2 border-[#E8D9C9] bg-white text-[#332820] transition hover:bg-[#F7F0E6]"
        aria-label="ตะกร้าสินค้า"
        @click="closeMenu"
      >
        <i class="mdi mdi-shopping-outline text-xl text-[#D96C2C]"></i>
        <span
          v-if="auth.isLoggedIn && cart.itemCount"
          class="absolute -top-1.5 -right-1.5 flex h-5 min-w-5 items-center justify-center rounded-full border-2 border-white bg-[#D96C2C] px-1 text-[10px] font-black text-white"
        >
          {{ cart.itemCount }}
        </span>
      </RouterLink>

      <button
        class="flex h-10 w-10 items-center justify-center rounded-xl border-2 border-[#E8D9C9] bg-[#F7F0E6] text-[#332820] transition hover:bg-[#E8D9C9]/50 cursor-pointer"
        :aria-expanded="isMobileMenuOpen"
        :aria-label="isMobileMenuOpen ? 'ปิดเมนู' : 'เปิดเมนู'"
        @click="isMobileMenuOpen = !isMobileMenuOpen"
      >
        <svg
          v-if="!isMobileMenuOpen"
          class="h-6 w-6 text-[#D96C2C]"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <path d="M4 6h16M4 12h16M4 18h16" stroke-linecap="round" />
        </svg>

        <svg
          v-else
          class="h-6 w-6 text-[#D96C2C]"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          stroke-width="2"
        >
          <path d="m6 6 12 12M18 6 6 18" stroke-linecap="round" />
        </svg>
      </button>
      </div>
    </nav>

    <!-- Mobile Navigation Drawer -->
    <Transition
      enter-active-class="transition duration-200 ease-out"
      enter-from-class="-translate-y-2 opacity-0"
      leave-active-class="transition duration-150 ease-in"
      leave-to-class="-translate-y-2 opacity-0"
    >
      <div
        v-if="isMobileMenuOpen"
        class="max-h-[calc(100dvh-4rem)] overflow-y-auto overscroll-contain border-t-2 border-[#E8D9C9] bg-[#FFF9F2] px-4 py-5 shadow-xl sm:max-h-[calc(100dvh-5rem)] sm:px-6 lg:hidden"
      >
        <div class="flex flex-col gap-1">
          <RouterLink
            v-for="item in navigationItems"
            :key="item.label"
            :to="item.to"
            class="mobile-nav-link"
            @click="closeMenu"
          >
            {{ item.label }}
          </RouterLink>
        </div>

        <div class="mt-5 flex flex-col gap-3 border-t-2 border-[#E8D9C9] pt-5">
          <!-- Mobile Create Content CTA Button -->
          <RouterLink
            to="/create"
            class="flex w-full items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] py-3 text-sm font-black text-white shadow-md transition active:scale-95 border border-[#D96C2C]/40"
            @click="closeMenu"
          >
            <i class="mdi mdi-plus-circle text-lg text-white"></i>
            <span class="text-white font-black">+ ลงคอนเทนต์ท่องเที่ยว</span>
          </RouterLink>

          <RouterLink
            to="/cart"
            class="login-button w-full justify-center gap-2"
            @click="closeMenu"
          >
            <i class="mdi mdi-shopping-outline text-lg text-[#D96C2C]"></i>
            <span>ตะกร้าสินค้า ({{ auth.isLoggedIn ? cart.itemCount : 0 }})</span>
          </RouterLink>

          <RouterLink
            v-if="!auth.isLoggedIn"
            to="/login"
            class="login-button w-full justify-center gap-2"
            @click="closeMenu"
          >
            <i class="mdi mdi-account-circle-outline text-lg text-[#D96C2C]"></i>
            <span>เข้าสู่ระบบ</span>
          </RouterLink>
          <template v-else>
            <RouterLink
              to="/profile"
              class="login-button w-full justify-center gap-2"
              @click="closeMenu"
            >
              <i class="mdi mdi-account-circle-outline text-lg text-[#D96C2C]"></i>
              <span>โปรไฟล์ของฉัน</span>
            </RouterLink>
            <button
              class="login-button w-full justify-center gap-2 hover:border-rose-200 hover:bg-rose-50 hover:text-rose-600 cursor-pointer"
              @click="logout"
            >
              <i class="mdi mdi-logout text-lg"></i>
              <span>ออกจากระบบ</span>
            </button>
          </template>
        </div>
      </div>
    </Transition>
  </header>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/auth'
import { useCartStore } from '@/stores/cart'

const isMobileMenuOpen = ref(false)
const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const cart = useCartStore()

async function loadCart() {
  if (!auth.isLoggedIn) {
    cart.clear()
    return
  }

  try {
    await cart.load()
  } catch {
    cart.clear()
  }
}

// Thin progress bar: how far down the page the visitor is
const scrollProgress = ref(0)
function updateProgress() {
  const max = document.documentElement.scrollHeight - window.innerHeight
  scrollProgress.value = max > 0 ? Math.min(100, Math.max(0, (window.scrollY / max) * 100)) : 0
}
onMounted(() => {
  updateProgress()
  window.addEventListener('scroll', updateProgress, { passive: true })
  window.addEventListener('resize', updateProgress, { passive: true })
})
onBeforeUnmount(() => {
  window.removeEventListener('scroll', updateProgress)
  window.removeEventListener('resize', updateProgress)
})
// Route changes swap the page height; recompute once the new page has rendered
watch(() => route.fullPath, () => setTimeout(updateProgress, 300))

onMounted(loadCart)
watch(() => auth.isLoggedIn, loadCart)
watch(() => route.fullPath, () => (isMobileMenuOpen.value = false))

const navigationItems = [
  { label: 'หน้าแรก', to: '/', icon: 'mdi-home-variant-outline' },
  { label: 'คอนเทนต์', to: '/contents', icon: 'mdi-book-open-page-variant-outline' },
  { label: 'ร้านค้า', to: '/shops', icon: 'mdi-storefront-outline' },
]

function closeMenu() {
  isMobileMenuOpen.value = false
}

function toggleSearch() {
  router.push('/contents')
}

function logout() {
  auth.logout()
  cart.clear()
  closeMenu()
  router.push('/login')
}
</script>

<style scoped>
.nav-link {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  border-radius: 9999px;
  padding: 0.5rem 1.1rem;
  color: #786B62;
  font-size: 1rem;
  font-weight: 700;
  transition: all 200ms ease;
}

.nav-link:hover {
  color: #D96C2C;
}

.nav-link.is-active {
  background: white;
  color: #D96C2C;
  box-shadow: 0 2px 8px rgb(51 40 32 / 0.1);
}

.nav-link:focus-visible {
  outline: none;
  box-shadow: 0 0 0 4px rgb(217 108 44 / 0.25);
}

.login-button {
  display: inline-flex;
  align-items: center;
  border: 1px solid #E8D9C9;
  border-radius: 9999px;
  padding: 0.5rem 1.1rem;
  background: white;
  color: #332820;
  font-size: 0.95rem;
  font-weight: 700;
  transition: all 200ms ease;
}

.login-button:hover {
  border-color: #D96C2C;
  color: #D96C2C;
  background: #FFF1E8;
}

.mobile-nav-link {
  display: flex;
  align-items: center;
  border-radius: 0.8rem;
  padding: 0.85rem 1.1rem;
  color: #332820;
  font-size: 1.05rem;
  font-weight: 600;
  transition: all 150ms ease;
}

.mobile-nav-link:hover,
.mobile-nav-link.router-link-active {
  background: #FFF1E8;
  color: #D96C2C;
  font-weight: 700;
}
</style>
