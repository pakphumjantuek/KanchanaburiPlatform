<script setup lang="ts">
import axios from 'axios'
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getMyShop, type Shop } from '@/features/merchant/api/shopApi'
import { useAuthStore } from '@/features/auth/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const isOpen = ref(false)
const shop = ref<Shop | null>(null)

const statusBadge = computed(() => {
  if (!shop.value) {
    return {
      text: 'กำลังโหลด...',
      dotClass: 'bg-slate-400',
      sidebarBadgeClass: 'bg-slate-800 text-white border-slate-700',
      headerBadgeClass: 'bg-slate-100 text-slate-700 border-slate-200',
    }
  }
  if (shop.value.status === 'Active') {
    return {
      text: 'เปิดบริการสาธารณะ',
      dotClass: 'bg-emerald-400 animate-pulse',
      sidebarBadgeClass: 'bg-emerald-500/25 text-white border-emerald-400/50',
      headerBadgeClass: 'bg-emerald-50 text-emerald-800 border-emerald-300 font-bold',
    }
  }
  return {
    text: shop.value.status || 'รอการอนุมัติ',
    dotClass: 'bg-amber-400',
    sidebarBadgeClass: 'bg-amber-500/25 text-white border-amber-400/50',
    headerBadgeClass: 'bg-amber-50 text-amber-800 border-amber-300 font-bold',
  }
})

const isItemActive = (path: string) => {
  if (path === '/my-shop') {
    return route.path === '/my-shop'
  }
  return route.path.startsWith(path)
}

const closeMenu = () => {
  isOpen.value = false
}

function handleLogout() {
  auth.logout()
  router.push('/login')
}

const navSections = [
  {
    title: 'ภาพรวมร้านค้า',
    items: [
      {
        to: '/my-shop',
        label: 'แดชบอร์ดภาพรวม',
        icon: 'mdi-view-dashboard-outline',
      },
      {
        to: '/my-shop/info',
        label: 'ข้อมูลและโปรไฟล์ร้านค้า',
        icon: 'mdi-store-edit-outline',
      },
    ],
  },
  {
    title: 'สินค้าและคำสั่งซื้อ',
    items: [
      {
        to: '/my-shop/products',
        label: 'จัดการสินค้าและสต็อก',
        icon: 'mdi-package-variant-closed',
      },
      {
        to: '/my-shop/orders',
        label: 'รายการคำสั่งซื้อของร้าน',
        icon: 'mdi-clipboard-text-clock-outline',
      },
    ],
  },
  {
    title: 'การตลาดและการเงิน',
    items: [
      {
        to: '/my-shop/contents',
        label: 'คอนเทนต์โปรโมทร้านค้า',
        icon: 'mdi-bullhorn-outline',
      },
      {
        to: '/my-shop/payouts',
        label: 'ประวัติการรับเงินยอดขาย',
        icon: 'mdi-cash-multiple',
      },
      {
        to: '/my-shop/exports',
        label: 'ส่งออกรายงานร้านค้า',
        icon: 'mdi-file-download-outline',
      },
    ],
  },
]

onMounted(async () => {
  try {
    shop.value = await getMyShop()
  } catch (error) {
    if (!axios.isAxiosError(error) || error.response?.status !== 404) {
      console.error(error)
    }
  }
})
</script>

<template>
  <div class="flex min-h-screen bg-slate-50 text-slate-800 font-sans antialiased">
    <!-- Backdrop for Mobile Sidebar -->
    <Transition
      enter-active-class="transition-opacity duration-300 ease-out"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200 ease-in"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="isOpen"
        class="fixed inset-0 z-40 bg-[#071e18]/80 backdrop-blur-xs lg:hidden"
        @click="closeMenu"
      />
    </Transition>

    <!-- Sidebar Navigation (Exact w-80 width and padding like Admin) -->
    <aside
      class="fixed inset-y-0 left-0 z-50 flex w-80 flex-col bg-[#0a2520] text-emerald-50 border-r border-[#133d34] transition-transform duration-300 ease-in-out lg:sticky lg:top-0 lg:h-screen lg:w-80 lg:shrink-0 lg:translate-x-0 shadow-2xl overflow-y-auto scrollbar-thin"
      :class="isOpen ? 'translate-x-0' : '-translate-x-full'"
    >
      <!-- Sidebar Brand Header (Matching Admin layout p-5 border-b space-y-4) -->
      <div class="p-5 border-b border-[#133d34] space-y-4 bg-[#071e18]">
        <div class="flex items-center justify-between">
          <RouterLink to="/my-shop" class="flex items-center gap-3" @click="closeMenu">
            <div
              class="flex h-11 w-11 items-center justify-center rounded-xl bg-gradient-to-br from-emerald-500 to-teal-600 text-white shrink-0 shadow-md shadow-emerald-950/50"
            >
              <i class="mdi mdi-storefront text-2xl text-white"></i>
            </div>
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="font-bold text-lg tracking-tight text-white block truncate">
                  ศูนย์จัดการร้านค้า
                </span>
                <span class="rounded bg-[#0a2520] px-2 py-0.5 text-xs font-bold text-emerald-300 border border-[#16443a] uppercase">
                  Merchant
                </span>
              </div>
              <span class="text-xs font-bold text-white block mt-0.5 truncate">
                ระบบจัดการสถานประกอบการ
              </span>
            </div>
          </RouterLink>

          <button
            type="button"
            class="rounded-lg p-1.5 text-white hover:bg-[#133d34] hover:text-white lg:hidden cursor-pointer transition"
            @click="closeMenu"
          >
            <i class="mdi mdi-close text-2xl text-white"></i>
          </button>
        </div>

        <!-- Shop Profile Card (Matching Admin's User Profile Card styling) -->
        <div class="p-3.5 rounded-xl bg-[#0a2520]/80 border border-[#16443a] shadow-xs">
          <div class="flex items-center gap-3">
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-500/25 text-white font-bold text-sm shrink-0 border border-emerald-400/40"
            >
              {{ shop?.shopName ? shop.shopName.charAt(0) : 'S' }}
            </div>
            <div class="min-w-0 flex-1">
              <p class="font-bold text-sm text-white truncate" :title="shop?.shopName ?? 'ร้านค้าของคุณ'">
                {{ shop?.shopName ?? 'กำลังโหลดข้อมูลร้าน...' }}
              </p>
              <div class="flex items-center gap-1.5 mt-0.5">
                <span class="w-2 h-2 rounded-full" :class="statusBadge.dotClass"></span>
                <span class="text-xs text-white font-bold truncate">
                  {{ statusBadge.text }}
                </span>
              </div>
            </div>
          </div>

          <div class="flex items-center justify-between pt-2.5 mt-2.5 border-t border-[#133d34] text-xs font-bold text-white">
            <span class="truncate text-emerald-100 font-semibold">{{ shop?.categoryName ?? 'ร้านค้าชุมชนท้องถิ่น' }}</span>
            <RouterLink
              v-if="shop?.shopId"
              :to="`/shops/${shop.shopId}`"
              target="_blank"
              class="inline-flex items-center gap-1 text-white hover:text-emerald-200 underline underline-offset-2 shrink-0"
              title="ดูหน้าร้านจริงบนเว็บไซต์"
            >
              <span>หน้าร้านจริง</span>
              <i class="mdi mdi-open-in-new text-xs text-emerald-300"></i>
            </RouterLink>
          </div>
        </div>
      </div>

      <!-- Navigation Sections (Matching Admin layout p-4 space-y-5 flex-1) -->
      <nav class="p-4 space-y-5 flex-1">
        <div v-for="(section, sIdx) in navSections" :key="sIdx" class="space-y-2">
          <h3 class="px-3 text-xs font-bold uppercase tracking-wider text-emerald-300">
            {{ section.title }}
          </h3>

          <div class="space-y-1">
            <RouterLink
              v-for="item in section.items"
              :key="item.to"
              :to="item.to"
              class="group flex items-center gap-3 px-3.5 py-3 rounded-xl text-sm font-bold transition-colors"
              :class="
                isItemActive(item.to)
                  ? '!bg-emerald-600 !text-white !font-bold shadow-xs'
                  : 'text-white hover:bg-[#133d34]/70'
              "
              @click="closeMenu"
            >
              <i
                class="mdi text-xl shrink-0 transition-colors"
                :class="[
                  item.icon,
                  isItemActive(item.to) ? 'text-white' : 'text-emerald-300 group-hover:text-white'
                ]"
              ></i>
              <span class="truncate leading-snug">{{ item.label }}</span>
            </RouterLink>
          </div>
        </div>
      </nav>

      <!-- Sidebar Footer (Matching Admin layout p-4 border-t space-y-2) -->
      <div class="p-4 border-t border-[#133d34] space-y-2 bg-[#071e18]">
        <!-- Switch to Admin if user has Admin role -->
        <RouterLink
          v-if="auth.user?.role === 'Admin'"
          to="/admin/dashboard"
          class="flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-sm font-bold text-white bg-blue-900/60 hover:bg-blue-800/80 border border-blue-400/50 transition"
          title="สลับไปยังแดชบอร์ดแอดมิน"
          @click="closeMenu"
        >
          <i class="mdi mdi-shield-crown text-lg text-blue-300 shrink-0"></i>
          <span class="truncate">สลับไปแผงควบคุมแอดมิน</span>
        </RouterLink>

        <!-- Profile Link -->
        <RouterLink
          to="/profile"
          class="flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-sm font-bold text-white hover:bg-[#133d34]/60 transition"
          @click="closeMenu"
        >
          <i class="mdi mdi-account-circle-outline text-lg text-emerald-300 shrink-0"></i>
          <span class="truncate">โปรไฟล์ผู้ใช้งาน</span>
        </RouterLink>

        <!-- Public Home Link -->
        <RouterLink
          to="/"
          class="flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-sm font-bold text-white hover:bg-[#133d34]/60 transition"
          @click="closeMenu"
        >
          <i class="mdi mdi-web text-lg text-emerald-300 shrink-0"></i>
          <span class="truncate">หน้าเว็บไซต์หลัก</span>
        </RouterLink>

        <!-- Logout Button -->
        <button
          type="button"
          class="flex items-center gap-2.5 w-full px-3.5 py-2.5 rounded-xl text-sm font-bold text-rose-300 hover:text-white hover:bg-rose-950/40 transition cursor-pointer text-left"
          @click="handleLogout"
        >
          <i class="mdi mdi-logout-variant text-lg text-rose-400 shrink-0"></i>
          <span>ออกจากระบบ</span>
        </button>
      </div>
    </aside>

    <!-- CONTENT WRAPPER (Matching Admin layout) -->
    <div class="flex-1 min-w-0 flex flex-col">
      <!-- MOBILE TOP BAR (Matching Admin layout px-5 py-4) -->
      <header
        class="flex items-center justify-between border-b border-[#133d34] bg-[#0a2520] text-white px-5 py-4 lg:hidden shadow-xs"
      >
        <RouterLink to="/my-shop" class="flex items-center gap-3">
          <div
            class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-600 text-white font-bold"
          >
            <i class="mdi mdi-storefront text-lg"></i>
          </div>
          <span class="font-bold text-base text-white">
            กาญจนบุรี <span class="text-emerald-300 text-xs font-semibold uppercase">Merchant</span>
          </span>
        </RouterLink>

        <button
          type="button"
          class="p-2 text-white hover:bg-[#133d34] rounded-lg transition cursor-pointer"
          aria-label="เปิดเมนูร้านค้า"
          @click="isOpen = !isOpen"
        >
          <i class="mdi text-2xl" :class="isOpen ? 'mdi-close' : 'mdi-menu'"></i>
        </button>
      </header>

      <!-- DESKTOP TOP BAR (Matching Admin layout px-8 py-3.5 border-b border-slate-200 bg-white) -->
      <div
        class="hidden lg:flex items-center justify-between border-b border-slate-200 bg-white px-8 py-3.5 shadow-2xs"
      >
        <div class="flex items-center gap-3">
          <span
            class="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-emerald-50 border border-emerald-200 text-sm font-semibold text-emerald-800"
          >
            <i class="mdi mdi-storefront text-emerald-600 text-base"></i>
            ศูนย์จัดการร้านค้า (Merchant Portal)
          </span>

          <span class="text-sm text-slate-700 font-bold truncate">
            ร้าน: <span class="text-emerald-950 font-black">{{ shop?.shopName || 'กำลังโหลดข้อมูลร้าน...' }}</span>
          </span>

          <span
            class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold border shrink-0"
            :class="statusBadge.headerBadgeClass"
          >
            <span class="w-1.5 h-1.5 rounded-full" :class="statusBadge.dotClass"></span>
            <span>{{ statusBadge.text }}</span>
          </span>
        </div>

        <div class="flex items-center gap-3 text-sm">
          <RouterLink
            v-if="shop?.shopId"
            :to="`/shops/${shop.shopId}`"
            target="_blank"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl border border-emerald-200 bg-emerald-50/50 hover:bg-emerald-50 text-emerald-800 font-semibold transition shadow-2xs"
          >
            <i class="mdi mdi-open-in-new text-emerald-600 text-base"></i>
            <span>ดูหน้าร้านจริง</span>
          </RouterLink>

          <RouterLink
            v-if="auth.user?.role === 'Admin'"
            to="/admin/dashboard"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl border border-blue-200 bg-blue-50 hover:bg-blue-100 text-blue-700 font-semibold transition shadow-2xs"
          >
            <i class="mdi mdi-shield-crown text-blue-600 text-base"></i>
            <span>แผงควบคุมแอดมิน</span>
          </RouterLink>

          <RouterLink
            to="/"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 text-slate-700 font-medium transition"
          >
            <i class="mdi mdi-web text-slate-500 text-base"></i>
            <span>หน้าเว็บไซต์หลัก</span>
          </RouterLink>
        </div>
      </div>

      <!-- MAIN PAGE CONTENT VIEW (Matching Admin layout p-6 sm:p-8) -->
      <main class="flex-1 min-w-0 p-6 sm:p-8">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
a,
button,
[role="button"] {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

a:focus,
a:focus-visible,
a:active,
button:focus,
button:focus-visible,
button:active {
  outline: none !important;
}

.scrollbar-thin::-webkit-scrollbar {
  width: 5px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: #071e18;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #16443a;
  border-radius: 9999px;
}
.scrollbar-thin::-webkit-scrollbar-thumb:hover {
  background: #1f5a4e;
}
</style>



