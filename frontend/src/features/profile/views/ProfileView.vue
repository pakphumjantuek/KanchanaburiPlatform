<template>
  <div class="min-h-screen bg-[#F7F0E6] text-[#332820] pb-24 font-sans">
    <!-- Top Breadcrumb Bar -->
    <div class="border-b border-[#E8D9C9] bg-[#FFFFFF]">
      <div
        class="mx-auto flex max-w-[1800px] items-center justify-between px-4 py-3 sm:px-6 lg:px-10 text-xs font-semibold text-[#786B62]"
      >
        <nav class="flex items-center gap-1.5 flex-wrap">
          <RouterLink to="/" class="hover:text-[#D96C2C] transition flex items-center gap-1">
            <i class="mdi mdi-home-outline text-sm"></i>
            <span>หน้าแรก</span>
          </RouterLink>
          <i class="mdi mdi-chevron-right text-[#786B62]/50 text-xs"></i>
          <span class="text-[#786B62]">ศูนย์จัดการบัญชี</span>
          <i class="mdi mdi-chevron-right text-[#786B62]/50 text-xs"></i>
          <span class="font-black text-[#D96C2C] bg-[#D96C2C]/10 px-2 py-0.5 rounded-md">{{ getTabBreadcrumbTitle(currentTab) }}</span>
        </nav>

        <div class="hidden sm:flex items-center gap-2">
          <span
            class="inline-flex items-center gap-1 text-[11px] font-black text-[#D96C2C] bg-[#D96C2C]/10 px-3 py-1 rounded-full border border-[#D96C2C]/20 shadow-2xs"
          >
            <i class="mdi mdi-shield-check text-xs"></i>
            ระบบจัดการบัญชีสมาชิก Kanchanaburi
          </span>
        </div>
      </div>
    </div>

    <!-- MAIN TWO-COLUMN CONTAINER -->
    <main class="mx-auto w-full max-w-[1800px] px-4 py-6 sm:px-6 sm:py-8 lg:px-10">
      <!-- Loading Skeleton -->
      <div v-if="loading" class="space-y-6">
        
        <div class="grid gap-8 lg:grid-cols-[19rem_minmax(0,1fr)]">
          <div class="h-96 animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFFFFF]" />
          <div class="h-96 animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFFFFF]" />
        </div>
      </div>

      <div v-else class="space-y-6">
        <!-- MOBILE HORIZONTAL NAVIGATION TABS (Shown on < lg screens for modern quick switching) -->
        <nav
          class="lg:hidden flex items-center gap-2 overflow-x-auto pb-1 no-scrollbar -mx-4 px-4 sm:mx-0 sm:px-0"
          aria-label="เมนูแท็บบนมือถือ"
        >
          <button
            v-for="t in tabItems"
            :key="t.tab"
            type="button"
            class="flex shrink-0 cursor-pointer items-center gap-1.5 whitespace-nowrap rounded-full border px-4 py-2 text-xs font-black transition-all focus-visible:ring-4 focus-visible:ring-[#D96C2C]/25 focus-visible:outline-none"
            :class="
              currentTab === t.tab
                ? 'border-[#D96C2C] bg-gradient-to-r from-[#D96C2C] to-[#BF5720] text-white shadow-md shadow-[#D96C2C]/25'
                : 'border-[#E8D9C9] bg-white text-[#332820] hover:border-[#D96C2C]'
            "
            @click="currentTab = t.tab"
          >
            <i class="mdi text-sm" :class="t.icon"></i>
            <span>{{ t.label }}<template v-if="t.count !== null"> ({{ t.count }})</template></span>
          </button>
        </nav>

        <!-- Content Grid (Desktop 2 Columns) -->
        <div class="grid items-start gap-6 lg:grid-cols-[17rem_minmax(0,1fr)] lg:gap-8 xl:grid-cols-[19rem_minmax(0,1fr)]">
          <!-- LEFT COLUMN: SIDEBAR NAVIGATION -->
          <div class="lg:sticky lg:top-28">
            <ProfileSidebar
              v-model:current-tab="currentTab"
              :profile="profile"
              :content-count="myContents.length"
              :favorite-count="favorites.length"
              :history-count="history.length"
              :order-count="orders.length"
              :shop="shop"
              @logout="handleLogout"
            />
          </div>

          <!-- RIGHT COLUMN: DYNAMIC TAB CONTENT WITH SMOOTH TRANSITION -->
          <div class="min-w-0">
            <Transition name="tab-fade" mode="out-in">
              <!-- 1. ข้อมูลส่วนตัว / แก้ไขโปรไฟล์ -->
              <ProfileInfoForm
                v-if="currentTab === 'profile'"
                key="tab-profile"
                :profile="profile"
                :saving="saving"
                @save="save"
              />

              <!-- 2. สมุดที่อยู่จัดส่ง -->
              <ProfileAddressList
                v-else-if="currentTab === 'addresses'"
                key="tab-addresses"
                :addresses="addresses"
                @changed="loadAddresses"
              />

              <!-- 3. กิจกรรมและคอนเทนต์ของคุณ -->
              <ProfileMyContents
                v-else-if="currentTab === 'contents'"
                key="tab-contents"
                :contents="myContents"
                @delete-content="deleteContent"
              />

              <!-- 4. รายการโปรดที่บันทึกไว้ -->
              <ProfileFavoritesList
                v-else-if="currentTab === 'favorites'"
                key="tab-favorites"
                :favorites="favorites"
              />

              <!-- 5. ประวัติการดู -->
              <ProfileHistoryList
                v-else-if="currentTab === 'history'"
                key="tab-history"
                :history="history"
              />

              <!-- 6. การซื้อของฉัน (เปิดใน Sidebar ได้ทันที) -->
              <ProfileOrdersList
                v-else-if="currentTab === 'orders'"
                key="tab-orders"
                :orders="orders"
              />

              <!-- 7. ร้านค้าของฉัน (เปิดใน Sidebar ได้ทันที) -->
              <ProfileMyShop
                v-else-if="currentTab === 'shop'"
                key="tab-shop"
                :shop="shop"
              />
            </Transition>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup lang="ts">
// Public storefront - Orange + Cream Profile Dashboard (Sidebar Navigation Pattern)
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import http from '@/shared/api/http'
import {
  getContentViewHistory,
  getFavoriteContents,
  getProfile,
  updateProfile,
  type ContentViewHistory,
  type FavoriteContent,
} from '../api/profileApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { useAuthStore } from '@/features/auth/stores/auth'
import {
  archiveMyContent,
  getMyContents,
  type UserContent,
} from '@/features/contents/user/api/userContentApi'
import { getUserAddresses, type UserAddress } from '@/features/cart/api/userAddressApi'

import ProfileSidebar, { type ProfileTab } from '../components/ProfileSidebar.vue'
import ProfileInfoForm from '../components/ProfileInfoForm.vue'
import ProfileMyContents from '../components/ProfileMyContents.vue'
import ProfileHistoryList from '../components/ProfileHistoryList.vue'
import ProfileFavoritesList from '../components/ProfileFavoritesList.vue'
import ProfileAddressList from '../components/ProfileAddressList.vue'
import ProfileOrdersList, { type RichOrder } from '../components/ProfileOrdersList.vue'
import ProfileMyShop from '../components/ProfileMyShop.vue'
import { getMyShop } from '@/features/merchant/api/shopApi'
import type { Shop } from '@/features/shops/shared/types/shop'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const swal = useSwal()

const loading = ref(true)
const saving = ref(false)
const currentTab = ref<ProfileTab>('orders')

const profile = reactive({
  id: '',
  firstName: '',
  lastName: '',
  name: '',
  email: '',
  role: '',
})

const history = ref<ContentViewHistory[]>([])
const myContents = ref<UserContent[]>([])
const favorites = ref<FavoriteContent[]>([])
const addresses = ref<UserAddress[]>([])
const orders = ref<RichOrder[]>([])
const shop = ref<Shop | null>(null)

const tabItems = computed(() => [
  { tab: 'contents' as ProfileTab, label: 'คอนเทนต์', icon: 'mdi-text-box-edit-outline', count: myContents.value.length as number | null },
  { tab: 'orders' as ProfileTab, label: 'คำสั่งซื้อ', icon: 'mdi-package-variant-closed', count: orders.value.length as number | null },
  { tab: 'profile' as ProfileTab, label: 'ข้อมูลส่วนตัว', icon: 'mdi-card-account-details-outline', count: null as number | null },
  { tab: 'addresses' as ProfileTab, label: 'ที่อยู่จัดส่ง', icon: 'mdi-map-marker-radius-outline', count: null as number | null },
  { tab: 'favorites' as ProfileTab, label: 'รายการโปรด', icon: 'mdi-heart-outline', count: favorites.value.length as number | null },
  { tab: 'history' as ProfileTab, label: 'ประวัติการดู', icon: 'mdi-clock-check-outline', count: null as number | null },
  { tab: 'shop' as ProfileTab, label: shop.value ? 'ร้านค้าของฉัน' : '+ เปิดร้านค้า', icon: 'mdi-storefront-outline', count: null as number | null },
])
function getTabBreadcrumbTitle(tab: ProfileTab) {
  if (tab === 'profile') return 'ข้อมูลส่วนตัว'
  if (tab === 'addresses') return 'ที่อยู่จัดส่งของฉัน'
  if (tab === 'contents') return 'กิจกรรมและคอนเทนต์'
  if (tab === 'favorites') return 'รายการโปรดที่บันทึกไว้'
  if (tab === 'history') return 'ประวัติการเข้าชม'
  if (tab === 'orders') return 'การซื้อของฉัน'
  if (tab === 'shop') return 'ร้านค้าของฉัน'
  return 'บัญชีของฉัน'
}

async function load() {
  loading.value = true
  try {
    const [data, views, submitted, saved, savedAddresses, myOrders, myShop] = await Promise.all([
      getProfile(),
      getContentViewHistory().catch(() => []),
      getMyContents({ page: 1, pageSize: 100 }).catch(() => ({ items: [] })),
      getFavoriteContents().catch(() => []),
      getUserAddresses().catch(() => []),
      http
        .get<RichOrder[]>('/orders/mine')
        .then((r) => r.data)
        .catch(() => []),
      getMyShop().catch(() => null),
    ])
    Object.assign(profile, data)
    history.value = views
    myContents.value = submitted.items
    favorites.value = saved
    addresses.value = savedAddresses
    orders.value = myOrders
    shop.value = myShop

    // If query ?tab=shop or others, switch to that tab
    if (
      route.query.tab &&
      ['profile', 'addresses', 'contents', 'favorites', 'history', 'orders', 'shop'].includes(
        String(route.query.tab),
      )
    ) {
      currentTab.value = route.query.tab as ProfileTab
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

async function loadAddresses() {
  try {
    addresses.value = await getUserAddresses()
  } catch (error) {
    await swal.error(
      'โหลดที่อยู่ไม่สำเร็จ',
      getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'),
    )
  }
}

watch(
  () => route.query.tab,
  (newTab) => {
    if (
      newTab &&
      ['profile', 'addresses', 'contents', 'favorites', 'history', 'orders', 'shop'].includes(
        String(newTab),
      )
    ) {
      currentTab.value = newTab as ProfileTab
    }
  },
)

async function save() {
  if (!profile.firstName.trim() || !profile.lastName.trim()) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณาระบุชื่อและนามสกุล')
    return
  }
  saving.value = true
  try {
    const updated = await updateProfile(profile)
    Object.assign(profile, updated)
    if (auth.user) {
      auth.user = { ...auth.user, name: updated.name }
      localStorage.setItem('authUser', JSON.stringify(auth.user))
    }
    await swal.success(
      'บันทึกข้อมูลส่วนตัวสำเร็จ',
      'ข้อมูลส่วนตัวของคุณได้รับการอัปเดตเรียบร้อยแล้ว',
    )
  } catch (error) {
    await swal.error('บันทึกโปรไฟล์ไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

async function deleteContent(item: UserContent) {
  const result = await swal.confirm(
    `ลบคอนเทนต์ "${item.title}"?`,
    'เรื่องราวนี้จะไม่แสดงต่อสาธารณะบนแพลตฟอร์มอีกต่อไป',
  )
  if (!result.isConfirmed) return
  try {
    await archiveMyContent(item.contentId)
    myContents.value = myContents.value.filter((c) => c.contentId !== item.contentId)
    await swal.success('ลบคอนเทนต์เรียบร้อยแล้ว')
  } catch (error) {
    await swal.error('ลบไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function handleLogout() {
  const result = await swal.confirm(
    'ออกจากระบบหรือไม่?',
    'คุณต้องการออกจากระบบบัญชีผู้ใช้นี้ใช่หรือไม่',
  )
  if (result.isConfirmed) {
    auth.logout()
    await router.push('/')
  }
}

onMounted(load)
</script>

<style scoped>
.tab-fade-enter-active,
.tab-fade-leave-active {
  transition: opacity 0.2s cubic-bezier(0.16, 1, 0.3, 1), transform 0.2s cubic-bezier(0.16, 1, 0.3, 1);
}

.tab-fade-enter-from {
  opacity: 0;
  transform: translateY(8px);
}

.tab-fade-leave-to {
  opacity: 0;
  transform: translateY(-8px);
}
</style>
