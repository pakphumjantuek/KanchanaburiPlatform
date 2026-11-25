<template>
  <main class="mx-auto w-full max-w-[1920px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
    <!-- HEADER -->
    <section
      class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white shadow-lg sm:p-8"
    >
      <div class="pointer-events-none absolute -right-16 -top-16 h-64 w-64 rounded-full bg-teal-400/20 blur-3xl" />
      <div class="pointer-events-none absolute -bottom-24 left-1/3 h-64 w-64 rounded-full bg-cyan-400/10 blur-3xl" />

      <div class="relative z-10 flex flex-col gap-6 md:flex-row md:items-center md:justify-between">
        <div class="flex items-center gap-4">
          <div
            class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border border-white/20 bg-white/10 text-3xl text-teal-300 backdrop-blur-md"
          >
            <i class="mdi mdi-storefront-outline" />
          </div>
          <div>
            <h1 class="text-2xl font-black tracking-tight sm:text-3xl">จัดการร้านค้า</h1>
            <p class="mt-1 text-sm font-bold text-slate-300">
              ตรวจสอบ อนุมัติ และเปลี่ยนสถานะร้านค้าทั้งหมดในระบบ
            </p>
          </div>
        </div>

        <div class="flex flex-wrap items-center gap-3">
          <button
            type="button"
            class="inline-flex cursor-pointer items-center gap-2 rounded-2xl border border-white/25 bg-white/10 px-4 py-2.5 text-sm font-bold text-white backdrop-blur-md transition hover:bg-white/20 active:scale-95"
            :disabled="loading"
            @click="refreshData"
          >
            <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
            <span>รีเฟรช</span>
          </button>

          <RouterLink
            to="/admin/shops/pending"
            class="inline-flex items-center gap-2 rounded-2xl bg-teal-400 px-5 py-2.5 text-sm font-black text-slate-900 shadow-md transition hover:bg-teal-300 active:scale-95"
          >
            <i class="mdi mdi-account-clock-outline text-lg" />
            <span>คำขอสมัครร้านค้า</span>
            <span
              v-if="stats.pending > 0"
              class="min-w-6 rounded-full bg-slate-900 px-2 py-0.5 text-center text-xs font-black text-teal-300"
            >
              {{ stats.pending }}
            </span>
          </RouterLink>
        </div>
      </div>
    </section>

    <!-- STATS KPI CARDS (คลิกเพื่อกรอง) -->
    <section class="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
      <button
        v-for="card in statCards"
        :key="card.value"
        type="button"
        class="group relative cursor-pointer overflow-hidden rounded-2xl border bg-white p-4 text-left shadow-xs transition hover:-translate-y-0.5 hover:shadow-md"
        :class="activeTab === card.value ? [card.activeBorder, 'ring-4', card.ring] : 'border-slate-300'"
        @click="selectTab(card.value)"
      >
        <span class="absolute inset-y-0 left-0 w-1.5" :class="card.bar" />
        <div class="flex items-start justify-between gap-2 pl-1.5">
          <div class="min-w-0">
            <p class="truncate text-xs font-black tracking-wide text-slate-600">{{ card.label }}</p>
            <p class="mt-1.5 text-3xl font-black leading-none" :class="card.text">
              {{ getTabCount(card.value) }}
            </p>
          </div>
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-xl" :class="card.iconBox">
            <i :class="['mdi', card.icon]" />
          </div>
        </div>
        <p class="mt-3 truncate pl-1.5 text-xs font-bold text-slate-500">{{ card.hint }}</p>
      </button>
    </section>

    <!-- SEARCH & FILTER -->
    <section class="rounded-2xl border border-slate-300 bg-white p-3 shadow-xs sm:p-4">
      <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div class="flex gap-1.5 overflow-x-auto rounded-2xl bg-slate-100 p-1.5">
          <button
            v-for="tab in statusTabs"
            :key="tab.value"
            type="button"
            class="inline-flex shrink-0 cursor-pointer items-center gap-2 rounded-xl px-3.5 py-2 text-xs font-bold transition sm:text-sm"
            :class="
              activeTab === tab.value
                ? 'bg-white text-teal-800 shadow-sm ring-1 ring-slate-300'
                : 'text-slate-600 hover:bg-white/60 hover:text-slate-900'
            "
            @click="selectTab(tab.value)"
          >
            <i :class="['mdi text-base', tab.icon]" />
            <span>{{ tab.label }}</span>
            <span
              class="rounded-full px-2 py-0.5 text-xs font-black"
              :class="activeTab === tab.value ? 'bg-teal-600 text-white' : 'bg-slate-200 text-slate-700'"
            >
              {{ getTabCount(tab.value) }}
            </span>
          </button>
        </div>

        <div class="relative w-full lg:w-96">
          <i class="mdi mdi-magnify absolute left-4 top-1/2 -translate-y-1/2 text-xl text-slate-500" />
          <input
            v-model="search"
            type="text"
            placeholder="ค้นหาชื่อร้านค้า หรือเบอร์โทร..."
            class="w-full rounded-2xl border border-slate-300 bg-slate-50 py-2.5 pl-11 pr-10 text-sm font-bold text-slate-900 placeholder:font-bold placeholder:text-slate-500 focus:border-teal-600 focus:bg-white focus:outline-none focus:ring-4 focus:ring-teal-600/15"
            @input="onSearchInput"
          />
          <button
            v-if="search"
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 cursor-pointer text-slate-500 hover:text-slate-800"
            title="ล้างคำค้นหา"
            @click="clearSearch"
          >
            <i class="mdi mdi-close-circle text-lg" />
          </button>
        </div>
      </div>
    </section>

    <!-- MAIN DATA TABLE -->
    <AppDataTable
      :columns="columns"
      :items="shops"
      row-key="shopId"
      :loading="loading"
      :pagination="pagination"
      :variant="'admin'"
      empty-message="ไม่พบข้อมูลร้านค้าตามเงื่อนไขที่เลือก"
      @page-change="changePage"
    >
      <!-- SHOP NAME & CATEGORY -->
      <template #cell-shopName="{ item }">
        <div class="flex items-center gap-3.5 py-1.5">
          <div
            class="flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-slate-300 bg-gradient-to-br from-teal-50 to-slate-100 text-teal-700 shadow-xs"
          >
            <img
              v-if="asShop(item).coverImageUrl"
              :src="asShop(item).coverImageUrl"
              alt="Cover"
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-storefront-outline text-2xl" />
          </div>
          <div class="min-w-0">
            <p class="truncate text-sm font-black text-slate-900">
              {{ asShop(item).shopName }}
            </p>
            <div class="mt-1 flex flex-wrap items-center gap-2">
              <span
                v-if="asShop(item).categoryName"
                class="rounded-lg border border-teal-200 bg-teal-50 px-2 py-0.5 text-xs font-bold text-teal-800"
              >
                {{ asShop(item).categoryName }}
              </span>
              <span class="font-mono text-xs font-bold text-slate-500">
                #{{ asShop(item).shopId.slice(0, 8) }}
              </span>
            </div>
          </div>
        </div>
      </template>

      <!-- CONTACT INFO -->
      <template #cell-contact="{ item }">
        <div class="space-y-1 text-xs">
          <p v-if="asShop(item).phone" class="flex items-center gap-1.5 font-black text-slate-800">
            <i class="mdi mdi-phone text-sm text-teal-700" />
            <span>{{ asShop(item).phone }}</span>
          </p>
          <p v-if="asShop(item).email" class="flex items-center gap-1.5 font-bold text-slate-600">
            <i class="mdi mdi-email-outline text-sm text-slate-500" />
            <span class="max-w-[200px] truncate">{{ asShop(item).email }}</span>
          </p>
          <span v-if="!asShop(item).phone && !asShop(item).email" class="font-bold text-slate-500">-</span>
        </div>
      </template>

      <!-- LOCATION -->
      <template #cell-location="{ item }">
        <div class="flex items-start gap-1.5 text-xs">
          <i class="mdi mdi-map-marker-outline mt-0.5 text-base text-teal-700" />
          <div>
            <p class="font-black text-slate-800">
              {{ asShop(item).districtName || 'ไม่ระบุอำเภอ' }}
            </p>
            <p class="font-bold text-slate-600">
              {{ asShop(item).subDistrictName || '-' }}
            </p>
          </div>
        </div>
      </template>

      <!-- STATUS BADGE -->
      <template #cell-status="{ item }">
        <span
          class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-black"
          :class="statusBadgeClass(asShop(item).status)"
        >
          <i :class="['mdi text-sm', statusIcon(asShop(item).status)]" />
          {{ statusLabel(asShop(item).status) }}
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex flex-wrap items-center gap-2">
          <button
            type="button"
            class="inline-flex cursor-pointer items-center gap-1.5 rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-700 shadow-xs transition hover:border-teal-600 hover:bg-teal-50 hover:text-teal-800"
            title="ดูรายละเอียดร้านค้า"
            @click="openDetail(asShop(item))"
          >
            <i class="mdi mdi-eye-outline text-base" />
            <span>ดูข้อมูล</span>
          </button>

          <RouterLink
            :to="`/admin/shops/${asShop(item).shopId}/products`"
            class="inline-flex items-center gap-1.5 whitespace-nowrap rounded-xl bg-teal-700 px-3 py-2 text-xs font-bold text-white shadow-xs transition hover:bg-teal-800 active:scale-95"
            title="จัดการสินค้าของร้านนี้"
          >
            <i class="mdi mdi-package-variant-closed text-base" />
            <span>สินค้า</span>
          </RouterLink>

          <select
            :value="asShop(item).status"
            :disabled="updatingId === asShop(item).shopId"
            class="w-36 cursor-pointer rounded-xl border border-slate-300 bg-white px-3 py-2 text-xs font-bold text-slate-800 shadow-xs focus:border-teal-600 focus:outline-none focus:ring-4 focus:ring-teal-600/15 disabled:cursor-wait disabled:opacity-50"
            title="เปลี่ยนสถานะร้านค้า"
            @change="changeStatus(asShop(item), ($event.target as HTMLSelectElement).value)"
          >
            <option v-for="opt in statusOptions" :key="opt.value" :value="opt.value">
              {{ opt.label }}
            </option>
          </select>
        </div>
      </template>
    </AppDataTable>

    <!-- SHOP DETAIL MODAL -->
    <Teleport to="body">
      <div
        v-if="selectedShop"
        class="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/70 p-4 backdrop-blur-sm"
        @click.self="selectedShop = null"
      >
        <div
          class="relative max-h-[92vh] w-full max-w-2xl overflow-y-auto rounded-3xl border border-slate-300 bg-white shadow-2xl"
        >
          <!-- MODAL HEADER -->
          <div class="relative bg-gradient-to-br from-slate-900 via-slate-800 to-teal-900 p-6 text-white">
            <button
              type="button"
              class="absolute right-4 top-4 flex h-9 w-9 cursor-pointer items-center justify-center rounded-xl bg-white/10 text-white transition hover:bg-white/25"
              title="ปิด"
              @click="selectedShop = null"
            >
              <i class="mdi mdi-close text-xl" />
            </button>

            <div class="flex items-center gap-4 pr-10">
              <div
                class="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-white/25 bg-white/10 text-3xl text-teal-300"
              >
                <img
                  v-if="selectedShop.coverImageUrl"
                  :src="selectedShop.coverImageUrl"
                  alt="Cover"
                  class="h-full w-full object-cover"
                />
                <i v-else class="mdi mdi-storefront-outline" />
              </div>
              <div class="min-w-0">
                <h2 class="truncate text-xl font-black sm:text-2xl">{{ selectedShop.shopName }}</h2>
                <div class="mt-1.5 flex flex-wrap items-center gap-2">
                  <span
                    class="inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-black"
                    :class="statusBadgeClass(selectedShop.status)"
                  >
                    <i :class="['mdi text-sm', statusIcon(selectedShop.status)]" />
                    {{ statusLabel(selectedShop.status) }}
                  </span>
                  <span class="font-mono text-xs font-bold text-slate-300">#{{ selectedShop.shopId.slice(0, 8) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- MODAL CONTENT -->
          <div class="space-y-4 p-6 text-sm text-slate-700">
            <div v-if="selectedShop.description" class="rounded-2xl border border-slate-300 bg-slate-50 p-4">
              <p class="mb-1 flex items-center gap-1.5 text-xs font-black text-slate-600">
                <i class="mdi mdi-text-box-outline text-base text-teal-700" /> คำอธิบายร้านค้า
              </p>
              <p class="whitespace-pre-line font-bold leading-relaxed text-slate-800">{{ selectedShop.description }}</p>
            </div>

            <div class="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-shape-outline text-base text-teal-700" /> หมวดหมู่ร้านค้า
                </p>
                <p class="mt-1.5 font-black text-slate-900">{{ selectedShop.categoryName || '-' }}</p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-clock-outline text-base text-teal-700" /> เวลาเปิด-ปิดทำการ
                </p>
                <p class="mt-1.5 font-black text-slate-900">
                  <span v-if="selectedShop.openingTime && selectedShop.closingTime">
                    {{ selectedShop.openingTime }} - {{ selectedShop.closingTime }} น.
                  </span>
                  <span v-else class="font-bold text-slate-500">ไม่ระบุเวลาทำการ</span>
                </p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-phone-outline text-base text-teal-700" /> เบอร์โทรศัพท์
                </p>
                <p class="mt-1.5 font-black text-slate-900">{{ selectedShop.phone || '-' }}</p>
              </div>
              <div class="rounded-2xl border border-slate-300 bg-white p-4">
                <p class="flex items-center gap-1.5 text-xs font-black text-slate-600">
                  <i class="mdi mdi-email-outline text-base text-teal-700" /> อีเมลติดต่อ
                </p>
                <p class="mt-1.5 break-all font-black text-slate-900">{{ selectedShop.email || '-' }}</p>
              </div>
            </div>

            <!-- LOCATION -->
            <div class="rounded-2xl border border-slate-300 bg-white p-4">
              <p class="mb-1 flex items-center gap-1.5 text-xs font-black text-slate-600">
                <i class="mdi mdi-map-marker-outline text-base text-teal-700" /> ที่ตั้งร้านค้า
              </p>
              <p class="font-black text-slate-900">
                {{ selectedShop.address || 'ไม่ระบุที่อยู่' }}
              </p>
              <p class="mt-1 text-xs font-bold text-slate-600">
                อำเภอ: {{ selectedShop.districtName || '-' }} | ตำบล: {{ selectedShop.subDistrictName || '-' }}
              </p>
            </div>

            <!-- BANK & PROMPTPAY -->
            <div class="rounded-2xl border border-teal-200 bg-teal-50/60 p-4">
              <p class="mb-3 flex items-center gap-1.5 text-xs font-black text-teal-900">
                <i class="mdi mdi-bank-outline text-base" /> ข้อมูลบัญชีรับเงิน
              </p>
              <div class="grid grid-cols-1 gap-3 text-xs sm:grid-cols-2">
                <div>
                  <p class="font-bold text-slate-600">ธนาคาร</p>
                  <p class="mt-0.5 text-sm font-black text-slate-900">{{ selectedShop.bankName || '-' }}</p>
                </div>
                <div>
                  <p class="font-bold text-slate-600">ชื่อบัญชี</p>
                  <p class="mt-0.5 text-sm font-black text-slate-900">{{ selectedShop.bankAccountName || '-' }}</p>
                </div>
                <div>
                  <p class="font-bold text-slate-600">เลขบัญชี</p>
                  <p class="mt-0.5 font-mono text-sm font-black text-slate-900">{{ selectedShop.bankAccountNumber || '-' }}</p>
                </div>
                <div>
                  <p class="font-bold text-slate-600">พร้อมเพย์</p>
                  <p class="mt-0.5 font-mono text-sm font-black text-slate-900">{{ selectedShop.promptPay || '-' }}</p>
                </div>
              </div>
            </div>
          </div>

          <!-- MODAL ACTIONS -->
          <div class="sticky bottom-0 flex flex-wrap items-center justify-between gap-3 border-t border-slate-300 bg-white/95 px-6 py-4 backdrop-blur">
            <RouterLink
              :to="`/admin/shops/${selectedShop.shopId}/products`"
              class="inline-flex items-center gap-1.5 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-bold text-white shadow-xs transition hover:bg-teal-800 active:scale-95"
            >
              <i class="mdi mdi-package-variant-closed text-lg" />
              <span>ดูรายการสินค้าทั้งหมด</span>
            </RouterLink>

            <button
              type="button"
              class="cursor-pointer rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 transition hover:bg-slate-100"
              @click="selectedShop = null"
            >
              ปิดหน้าต่าง
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { getAdminShops, updateShopStatus } from '../api/adminShopApi'
import type { Shop } from '@/features/shops/shared/types/shop'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const shops = ref<Shop[]>([])
const search = ref('')
const activeTab = ref<string>('All')
const loading = ref(true)
const updatingId = ref<string | null>(null)
const selectedShop = ref<Shop | null>(null)

const pagination = ref<DataTablePagination>({
  page: 1,
  pageSize: 15,
  totalCount: 0,
  totalPages: 0,
})

const stats = reactive({
  all: 0,
  active: 0,
  pending: 0,
  suspended: 0,
  closed: 0,
})

const swal = useSwal()

const statusOptions = [
  { value: 'Active', label: 'เปิดใช้งาน' },
  { value: 'PendingApproval', label: 'รออนุมัติ' },
  { value: 'Suspended', label: 'ระงับการใช้งาน' },
  { value: 'Rejected', label: 'ไม่อนุมัติ' },
  { value: 'Closed', label: 'ปิดร้าน' },
  { value: 'Inactive', label: 'ปิดใช้งาน' },
]

const statusTabs = [
  { value: 'All', label: 'ทั้งหมด', icon: 'mdi-layers-outline' },
  { value: 'Active', label: 'เปิดใช้งาน', icon: 'mdi-check-circle-outline' },
  { value: 'PendingApproval', label: 'รออนุมัติ', icon: 'mdi-clock-outline' },
  { value: 'Suspended', label: 'ระงับการใช้งาน', icon: 'mdi-shield-alert-outline' },
  { value: 'Closed', label: 'ปิดร้าน / ปิดใช้งาน', icon: 'mdi-store-off-outline' },
]

const statCards = [
  {
    value: 'All',
    label: 'ร้านค้าทั้งหมด',
    hint: 'ทุกสถานะในระบบ',
    icon: 'mdi-store-outline',
    bar: 'bg-slate-600',
    text: 'text-slate-900',
    iconBox: 'bg-slate-100 text-slate-700',
    activeBorder: 'border-slate-700',
    ring: 'ring-slate-500/15',
  },
  {
    value: 'Active',
    label: 'เปิดใช้งาน',
    hint: 'เปิดทำการปกติ',
    icon: 'mdi-check-circle-outline',
    bar: 'bg-emerald-600',
    text: 'text-emerald-700',
    iconBox: 'bg-emerald-100 text-emerald-700',
    activeBorder: 'border-emerald-600',
    ring: 'ring-emerald-500/15',
  },
  {
    value: 'PendingApproval',
    label: 'รออนุมัติ',
    hint: 'รอแอดมินพิจารณา',
    icon: 'mdi-clock-outline',
    bar: 'bg-amber-500',
    text: 'text-amber-700',
    iconBox: 'bg-amber-100 text-amber-700',
    activeBorder: 'border-amber-500',
    ring: 'ring-amber-500/15',
  },
  {
    value: 'Suspended',
    label: 'ระงับการใช้งาน',
    hint: 'ถูกระงับชั่วคราว',
    icon: 'mdi-shield-alert-outline',
    bar: 'bg-rose-600',
    text: 'text-rose-700',
    iconBox: 'bg-rose-100 text-rose-700',
    activeBorder: 'border-rose-600',
    ring: 'ring-rose-500/15',
  },
  {
    value: 'Closed',
    label: 'ปิดร้าน / ปิดใช้งาน',
    hint: 'ปิดร้านหรือปิดถาวร',
    icon: 'mdi-store-off-outline',
    bar: 'bg-slate-400',
    text: 'text-slate-700',
    iconBox: 'bg-slate-100 text-slate-600',
    activeBorder: 'border-slate-500',
    ring: 'ring-slate-400/15',
  },
]

const columns: DataTableColumn[] = [
  { key: 'shopName', label: 'ร้านค้า' },
  { key: 'contact', label: 'ข้อมูลติดต่อ' },
  { key: 'location', label: 'พื้นที่ตั้ง' },
  { key: 'status', label: 'สถานะ' },
  { key: 'actions', label: 'จัดการ' },
]

function asShop(item: object): Shop {
  return item as Shop
}

function getTabCount(tabValue: string): number {
  switch (tabValue) {
    case 'Active':
      return stats.active
    case 'PendingApproval':
      return stats.pending
    case 'Suspended':
      return stats.suspended
    case 'Closed':
      return stats.closed
    default:
      return stats.all
  }
}

function statusLabel(value: string): string {
  return statusOptions.find((opt) => opt.value === value)?.label ?? value
}

function statusBadgeClass(status: string): string {
  switch (status) {
    case 'Active':
      return 'bg-emerald-100 text-emerald-800 border border-emerald-300'
    case 'PendingApproval':
      return 'bg-amber-100 text-amber-800 border border-amber-300'
    case 'Suspended':
    case 'Rejected':
      return 'bg-rose-100 text-rose-800 border border-rose-300'
    case 'Closed':
    case 'Inactive':
      return 'bg-slate-200 text-slate-800 border border-slate-400'
    default:
      return 'bg-slate-100 text-slate-600 border border-slate-200'
  }
}

function statusIcon(status: string): string {
  switch (status) {
    case 'Active':
      return 'mdi-check-circle'
    case 'PendingApproval':
      return 'mdi-clock-outline'
    case 'Suspended':
      return 'mdi-shield-alert'
    case 'Rejected':
      return 'mdi-close-circle'
    case 'Closed':
    case 'Inactive':
      return 'mdi-store-off'
    default:
      return 'mdi-help-circle'
  }
}

async function loadStats() {
  try {
    const [allRes, activeRes, pendingRes, suspendedRes, closedRes] = await Promise.allSettled([
      getAdminShops({ page: 1, pageSize: 1 }),
      getAdminShops({ status: 'Active', page: 1, pageSize: 1 }),
      getAdminShops({ status: 'PendingApproval', page: 1, pageSize: 1 }),
      getAdminShops({ status: 'Suspended', page: 1, pageSize: 1 }),
      getAdminShops({ status: 'Closed', page: 1, pageSize: 1 }),
    ])

    if (allRes.status === 'fulfilled') stats.all = allRes.value.totalCount
    if (activeRes.status === 'fulfilled') stats.active = activeRes.value.totalCount
    if (pendingRes.status === 'fulfilled') stats.pending = pendingRes.value.totalCount
    if (suspendedRes.status === 'fulfilled') stats.suspended = suspendedRes.value.totalCount
    if (closedRes.status === 'fulfilled') stats.closed = closedRes.value.totalCount
  } catch {
    // Non-blocking stats error
  }
}

async function loadShops() {
  loading.value = true
  try {
    const result = await getAdminShops({
      search: search.value.trim() || undefined,
      status: activeTab.value === 'All' ? null : activeTab.value,
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
    })
    shops.value = result.items
    pagination.value = {
      page: result.page,
      pageSize: result.pageSize,
      totalCount: result.totalCount,
      totalPages: result.totalPages,
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลร้านค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

function selectTab(tab: string) {
  activeTab.value = tab
  pagination.value.page = 1
  loadShops()
}

function onSearchInput() {
  pagination.value.page = 1
  loadShops()
}

function clearSearch() {
  search.value = ''
  pagination.value.page = 1
  loadShops()
}

function changePage(page: number) {
  pagination.value.page = page
  loadShops()
}

function refreshData() {
  loadShops()
  loadStats()
}

function openDetail(shop: Shop) {
  selectedShop.value = shop
}

async function changeStatus(shop: Shop, nextStatus: string) {
  if (nextStatus === shop.status) return
  const result = await swal.confirm(`ต้องการเปลี่ยนสถานะร้านค้าเป็น "${statusLabel(nextStatus)}"?`, shop.shopName)
  if (!result.isConfirmed) return

  updatingId.value = shop.shopId
  try {
    await updateShopStatus(shop.shopId, nextStatus)
    shop.status = nextStatus
    await swal.success('อัปเดตสถานะร้านค้าเรียบร้อยแล้ว')
    await loadStats()
  } catch (error) {
    await swal.error('อัปเดตสถานะไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

onMounted(() => {
  loadShops()
  loadStats()
})
</script>
