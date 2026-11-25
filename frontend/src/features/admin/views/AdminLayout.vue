<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/features/auth/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const isMobileMenuOpen = ref(false)

function logout() {
  auth.logout()
  router.push('/login')
}

const navGroups = [
  {
    title: 'ภาพรวมระบบ',
    items: [
      {
        to: '/admin/dashboard',
        label: 'แดชบอร์ดภาพรวม (Overview)',
        icon: 'mdi-view-dashboard-outline',
      },
    ],
  },
  {
    title: 'การกำกับดูแลร้านค้าและการเงิน',
    items: [
      {
        to: '/admin/shops',
        label: 'จัดการร้านค้าทั้งหมด',
        icon: 'mdi-storefront-outline',
      },
      {
        to: '/admin/orders',
        label: 'ตรวจสอบสลิปชำระเงิน',
        icon: 'mdi-file-document-check-outline',
      },
      {
        to: '/admin/payouts',
        label: 'การโอนเงินเคลียร์ยอดขาย (Payouts)',
        icon: 'mdi-cash-refund',
      },
      {
        to: '/admin/categories',
        label: 'หมวดหมู่ร้านค้า',
        icon: 'mdi-shape-outline',
      },
      {
        to: '/admin/product-categories',
        label: 'หมวดหมู่สินค้า',
        icon: 'mdi-package-variant-closed',
      },
    ],
  },
  {
    title: 'คอนเทนต์และกิจกรรม',
    items: [
      {
        to: '/admin/contents',
        label: 'จัดการคอนเทนต์',
        icon: 'mdi-text-box-multiple-outline',
      },
      {
        to: '/admin/content-categories',
        label: 'หมวดหมู่คอนเทนต์',
        icon: 'mdi-folder-multiple-outline',
      },
      {
        to: '/admin/tags',
        label: 'แท็กคอนเทนต์',
        icon: 'mdi-tag-multiple-outline',
      },
      {
        to: '/admin/schedules',
        label: 'กำหนดการกิจกรรม',
        icon: 'mdi-calendar-clock-outline',
      },
    ],
  },
  {
    title: 'ความปลอดภัยและการตรวจสอบ',
    items: [
      {
        to: '/admin/reports',
        label: 'รายงานข้อร้องเรียน',
        icon: 'mdi-shield-alert-outline',
      },
    ],
  },
]
</script>

<template>
  <div class="flex min-h-screen bg-slate-100 text-slate-800 font-sans antialiased">
    <!-- DESKTOP SIDEBAR -->
    <aside
      class="hidden w-80 shrink-0 flex-col bg-slate-900 text-slate-100 border-r border-slate-800 lg:flex sticky top-0 h-screen overflow-y-auto scrollbar-thin z-30"
    >
      <!-- BRAND LOGO HEADER -->
      <div class="p-5 border-b border-slate-800 space-y-4 bg-slate-900/90">
        <RouterLink to="/admin" class="flex items-center gap-3">
          <div
            class="flex h-11 w-11 items-center justify-center rounded-xl bg-teal-600 text-white shrink-0 shadow-xs"
          >
            <i class="mdi mdi-shield-crown text-2xl"></i>
          </div>
          <div class="min-w-0">
            <div class="flex items-center gap-2">
              <span class="font-bold text-lg tracking-tight text-white block truncate">
                กาญจนบุรี
              </span>
              <span class="rounded bg-slate-800 px-2 py-0.5 text-xs font-bold text-slate-300 border border-slate-700 uppercase">
                Admin
              </span>
            </div>
            <span class="text-xs font-bold text-slate-400 block mt-0.5 truncate">
              ระบบบริหารจัดการส่วนกลาง
            </span>
          </div>
        </RouterLink>

        <!-- ADMIN USER PROFILE CARD -->
        <div class="p-3.5 rounded-xl bg-slate-800/70 border border-slate-700/60">
          <div class="flex items-center gap-3">
            <div
              class="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-700 text-slate-200 font-bold text-sm shrink-0"
            >
              {{ auth.user?.name?.charAt(0) || 'A' }}
            </div>
            <div class="min-w-0 flex-1">
              <p class="font-bold text-sm text-white truncate">
                {{ auth.user?.name || 'ผู้ดูแลระบบส่วนกลาง' }}
              </p>
              <div class="flex items-center gap-1.5 mt-0.5">
                <span class="w-2 h-2 rounded-full bg-emerald-500"></span>
                <span class="text-xs text-slate-400 font-bold">
                  Platform Administrator
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- NAVIGATION GROUPS -->
      <nav class="p-4 space-y-5 flex-1">
        <div v-for="(group, gIdx) in navGroups" :key="gIdx" class="space-y-2">
          <h3 class="px-3 text-xs font-bold uppercase tracking-wider text-slate-400">
            {{ group.title }}
          </h3>
          <div class="space-y-1">
            <RouterLink
              v-for="item in group.items"
              :key="item.to"
              :to="item.to"
              class="group flex items-center gap-3 px-3.5 py-3 rounded-xl text-sm font-bold text-slate-300 hover:text-white hover:bg-slate-800/70 transition-colors"
              active-class="!bg-teal-600 !text-white !font-bold shadow-xs"
            >
              <i
                class="mdi text-xl text-slate-400 group-hover:text-slate-200 group-[.router-link-active]:text-white shrink-0"
                :class="item.icon"
              ></i>
              <span class="truncate leading-snug">{{ item.label }}</span>
            </RouterLink>
          </div>
        </div>
      </nav>

      <!-- FOOTER ACTIONS -->
      <div class="p-4 border-t border-slate-800 space-y-2 bg-slate-900/95">
        <!-- Quick Switch to Merchant Portal -->
        <RouterLink
          to="/my-shop"
          class="flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-sm font-bold text-slate-300 hover:text-white hover:bg-slate-800/60 transition"
        >
          <i class="mdi mdi-storefront-outline text-lg text-slate-400 shrink-0"></i>
          <span>ศูนย์จัดการสถานประกอบการ</span>
        </RouterLink>

        <!-- Public Portal Link -->
        <RouterLink
          to="/"
          class="flex items-center gap-2.5 px-3.5 py-2.5 rounded-xl text-sm font-bold text-slate-300 hover:text-white hover:bg-slate-800/60 transition"
        >
          <i class="mdi mdi-web text-lg text-slate-400 shrink-0"></i>
          <span>หน้าเว็บไซต์หลัก</span>
        </RouterLink>

        <!-- Logout Button -->
        <button
          type="button"
          class="flex items-center gap-2.5 w-full px-3.5 py-2.5 rounded-xl text-sm font-bold text-rose-300 hover:text-rose-100 hover:bg-rose-950/30 transition cursor-pointer"
          @click="logout"
        >
          <i class="mdi mdi-logout-variant text-lg text-rose-400 shrink-0"></i>
          <span>ออกจากระบบ</span>
        </button>
      </div>
    </aside>

    <!-- CONTENT WRAPPER -->
    <div class="flex-1 min-w-0 flex flex-col">
      <!-- MOBILE TOP BAR -->
      <header
        class="flex items-center justify-between border-b border-slate-800 bg-slate-900 text-white px-5 py-4 lg:hidden shadow-xs"
      >
        <RouterLink to="/admin" class="flex items-center gap-3">
          <div
            class="flex h-9 w-9 items-center justify-center rounded-xl bg-teal-600 text-white font-bold"
          >
            <i class="mdi mdi-shield-crown text-lg"></i>
          </div>
          <span class="font-bold text-base text-white">
            กาญจนบุรี <span class="text-slate-400 text-xs font-bold uppercase">Admin</span>
          </span>
        </RouterLink>

        <button
          type="button"
          class="p-2 text-slate-300 hover:text-white hover:bg-slate-800 rounded-lg transition cursor-pointer"
          @click="isMobileMenuOpen = !isMobileMenuOpen"
        >
          <i class="mdi text-2xl" :class="isMobileMenuOpen ? 'mdi-close' : 'mdi-menu'"></i>
        </button>
      </header>

      <!-- MOBILE OVERLAY MENU -->
      <div
        v-if="isMobileMenuOpen"
        class="lg:hidden bg-slate-900 text-white border-b border-slate-800 p-5 space-y-4 shadow-xl"
      >
        <div v-for="(group, gIdx) in navGroups" :key="gIdx" class="space-y-2">
          <h3 class="px-2 text-xs font-bold uppercase text-slate-400">{{ group.title }}</h3>
          <RouterLink
            v-for="item in group.items"
            :key="item.to"
            :to="item.to"
            class="flex items-center gap-3 px-3.5 py-2.5 rounded-xl text-sm font-bold text-slate-200 hover:bg-slate-800"
            active-class="bg-teal-600 !text-white !font-bold"
            @click="isMobileMenuOpen = false"
          >
            <i class="mdi text-lg text-slate-400" :class="item.icon"></i>
            <span>{{ item.label }}</span>
          </RouterLink>
        </div>

        <div class="pt-3 border-t border-slate-800 flex flex-col gap-2">
          <RouterLink
            to="/my-shop"
            class="text-center py-2.5 rounded-xl bg-slate-800 text-slate-300 text-sm font-bold"
            @click="isMobileMenuOpen = false"
          >
            ศูนย์จัดการสถานประกอบการ
          </RouterLink>
          <div class="flex gap-2">
            <RouterLink
              to="/"
              class="flex-1 text-center py-2.5 rounded-xl bg-slate-800 text-sm font-bold text-slate-300"
              @click="isMobileMenuOpen = false"
            >
              หน้าเว็บไซต์หลัก
            </RouterLink>
            <button
              type="button"
              class="flex-1 text-center py-2.5 rounded-xl bg-rose-950/40 text-rose-300 text-sm font-bold"
              @click="logout"
            >
              ออกจากระบบ
            </button>
          </div>
        </div>
      </div>

      <!-- DESKTOP TOP BAR -->
      <div class="hidden lg:flex items-center justify-between border-b border-slate-300 bg-white px-8 py-3.5 shadow-sm">
        <div class="flex items-center gap-3">
          <span class="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-teal-50 border border-teal-200 text-sm font-bold text-teal-900">
            <i class="mdi mdi-shield-outline text-teal-700 text-base"></i>
            ระบบผู้ดูแลส่วนกลาง (Admin Portal)
          </span>
          <span class="text-sm text-slate-500 font-bold">
            การกำกับดูแลและบริหารจัดการแพลตฟอร์ม
          </span>
        </div>

        <div class="flex items-center gap-3 text-sm">
          <RouterLink
            to="/my-shop"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl border border-slate-300 bg-slate-50 hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800 text-slate-700 font-bold transition"
          >
            <i class="mdi mdi-storefront-outline text-slate-500 text-base"></i>
            <span>หน้าสถานประกอบการ</span>
          </RouterLink>
          <RouterLink
            to="/"
            class="inline-flex items-center gap-2 px-3.5 py-2 rounded-xl border border-slate-300 bg-slate-50 hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800 text-slate-700 font-bold transition"
          >
            <i class="mdi mdi-open-in-new text-slate-500 text-base"></i>
            <span>หน้าเว็บไซต์หลัก</span>
          </RouterLink>
        </div>
      </div>

      <!-- MAIN PAGE CONTENT VIEW -->
      <main class="flex-1 min-w-0">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.scrollbar-thin::-webkit-scrollbar {
  width: 5px;
}
.scrollbar-thin::-webkit-scrollbar-track {
  background: #0f172a;
}
.scrollbar-thin::-webkit-scrollbar-thumb {
  background: #334155;
  border-radius: 9999px;
}
</style>
