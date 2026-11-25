<script setup lang="ts">
import { computed } from 'vue'
import type { Shop } from '@/features/shops/api'

export type ProfileTab =
  'profile' | 'addresses' | 'contents' | 'favorites' | 'history' | 'orders' | 'shop'

interface Props {
  currentTab: ProfileTab
  profile: {
    firstName: string
    lastName: string
    email: string
    role: string
  }
  contentCount: number
  favoriteCount: number
  historyCount: number
  orderCount: number
  shop: Shop | null
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'update:currentTab': [tab: ProfileTab]
  logout: []
}>()

// Order matters: contents + orders are used most, so they sit at the top
const sections = computed(() => [
  {
    title: 'ใช้บ่อย',
    icon: 'mdi-star-circle-outline',
    items: [
      {
        tab: 'orders' as ProfileTab,
        label: 'คำสั่งซื้อของฉัน',
        icon: 'mdi-package-variant-closed',
        count: props.orderCount as number | null,
        badge: '',
      },
      {
        tab: 'contents' as ProfileTab,
        label: 'คอนเทนต์ของคุณ',
        icon: 'mdi-text-box-edit-outline',
        count: props.contentCount as number | null,
        badge: '',
      },
    ],
  },
  {
    title: 'บัญชีของฉัน',
    icon: 'mdi-account-circle-outline',
    items: [
      {
        tab: 'profile' as ProfileTab,
        label: 'ข้อมูลส่วนตัว (แก้ไข)',
        icon: 'mdi-card-account-details-outline',
        count: null as number | null,
        badge: '',
      },
      {
        tab: 'addresses' as ProfileTab,
        label: 'ที่อยู่จัดส่งของฉัน',
        icon: 'mdi-map-marker-radius-outline',
        count: null as number | null,
        badge: '',
      },
    ],
  },
  {
    title: 'รายการที่บันทึกไว้',
    icon: 'mdi-bookmark-outline',
    items: [
      {
        tab: 'favorites' as ProfileTab,
        label: 'รายการโปรด',
        icon: 'mdi-heart-outline',
        count: props.favoriteCount as number | null,
        badge: '',
      },
      {
        tab: 'history' as ProfileTab,
        label: 'ประวัติการดู',
        icon: 'mdi-clock-check-outline',
        count: props.historyCount as number | null,
        badge: '',
      },
    ],
  },
  {
    title: 'ร้านค้าของฉัน',
    icon: 'mdi-store-cog-outline',
    items: [
      {
        tab: 'shop' as ProfileTab,
        label: props.shop ? props.shop.shopName : 'เปิดร้านค้าของคุณ',
        icon: 'mdi-storefront-outline',
        count: null as number | null,
        badge: props.shop ? 'เปิดร้าน' : '+ สร้างรายได้',
      },
    ],
  },
])
</script>

<template>
  <aside class="space-y-4">
    <!-- Navigation: most-used items first -->
    <div class="space-y-4 overflow-hidden rounded-3xl border border-[#E8D9C9] bg-white p-3">
      <div
        v-for="(section, sIdx) in sections"
        :key="section.title"
        class="space-y-1"
        :class="sIdx > 0 ? 'border-t border-[#E8D9C9] pt-3' : ''"
      >
        <div
          class="flex items-center gap-2 px-3 py-1.5 text-[11px] font-black tracking-wider text-[#786B62]"
        >
          <i class="mdi text-sm text-[#D96C2C]" :class="section.icon"></i>
          <span>{{ section.title }}</span>
        </div>

        <button
          v-for="item in section.items"
          :key="item.tab"
          type="button"
          class="group flex w-full cursor-pointer items-center justify-between rounded-2xl px-3.5 py-3 text-left text-sm font-black transition-all focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
          :class="
            props.currentTab === item.tab
              ? 'bg-gradient-to-r from-[#D96C2C] to-[#BF5720] text-white shadow-md shadow-[#D96C2C]/25'
              : 'text-[#332820] hover:bg-[#F7F0E6] hover:text-[#D96C2C]'
          "
          @click="emit('update:currentTab', item.tab)"
        >
          <div class="flex min-w-0 flex-1 items-center gap-2.5">
            <span
              class="flex h-7 w-7 shrink-0 items-center justify-center rounded-xl transition"
              :class="
                props.currentTab === item.tab
                  ? 'bg-white/20 text-white'
                  : 'bg-[#E8D9C9]/50 text-[#786B62] group-hover:bg-[#D96C2C]/15 group-hover:text-[#D96C2C]'
              "
            >
              <i class="mdi text-sm" :class="item.icon"></i>
            </span>
            <span class="truncate">{{ item.label }}</span>
          </div>

          <span
            v-if="item.count !== null"
            class="rounded-full px-2 py-0.5 text-[11px] font-black transition"
            :class="
              props.currentTab === item.tab
                ? 'bg-white/25 text-white'
                : 'bg-[#E8D9C9] text-[#332820] group-hover:bg-[#D96C2C]/15 group-hover:text-[#D96C2C]'
            "
          >
            {{ item.count }}
          </span>
          <span
            v-else-if="item.badge"
            class="ml-1.5 shrink-0 rounded-full px-2.5 py-0.5 text-[10px] font-black transition"
            :class="
              props.currentTab === item.tab
                ? 'bg-white/25 text-white'
                : props.shop
                  ? 'border border-emerald-200 bg-emerald-100 text-emerald-800'
                  : 'border border-amber-200 bg-amber-100 text-amber-800'
            "
          >
            {{ item.badge }}
          </span>
        </button>
      </div>
    </div>
    <!-- Help Card Box -->
    <div
      class="relative overflow-hidden rounded-3xl border border-[#E8D9C9] bg-gradient-to-br from-[#FFF1E8] to-[#FFFFFF] p-4 text-xs font-semibold text-[#786B62] space-y-2 shadow-2xs"
    >
      <div class="flex items-center gap-2 font-black text-[#332820]">
        <span
          class="flex h-7 w-7 items-center justify-center rounded-xl bg-[#D96C2C]/10 text-[#D96C2C]"
        >
          <i class="mdi mdi-help-circle-outline text-base"></i>
        </span>
        <span>ต้องการความช่วยเหลือ?</span>
      </div>
      <p class="text-[11px] leading-relaxed">
        มีข้อสงสัยเกี่ยวกับการใช้งาน แจ้งปัญหา หรือสอบถามข้อมูลเพิ่มเติมได้ตลอด 24 ชม.
      </p>
      <RouterLink
        to="/"
        class="inline-flex items-center gap-1 font-black text-[#D96C2C] hover:underline pt-1 text-xs group"
      >
        <span>ศูนย์ช่วยเหลือ Kanchanaburi Platform</span>
        <i class="mdi mdi-arrow-right text-xs transition-transform group-hover:translate-x-1"></i>
      </RouterLink>
    </div>

    <!-- Logout Action Button -->
    <button
      type="button"
      class="group w-full flex items-center justify-center gap-2 rounded-2xl border border-rose-200 bg-rose-50/70 p-3 text-xs sm:text-sm font-black text-rose-600 transition hover:bg-rose-100 hover:border-rose-300 active:scale-95 cursor-pointer shadow-2xs"
      @click="emit('logout')"
    >
      <i class="mdi mdi-logout text-base transition-transform group-hover:-translate-x-0.5"></i>
      <span>ออกจากระบบ</span>
    </button>
  </aside>
</template>
