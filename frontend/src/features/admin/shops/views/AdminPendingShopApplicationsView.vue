<template>
  <main class="mx-auto w-full max-w-[1920px] px-6 py-8 lg:px-10 space-y-6">
    <!-- HEADER -->
    <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 rounded-3xl border border-slate-300 border-l-8 border-l-teal-600 bg-white p-5 shadow-xs sm:p-6">
      <div>
        <div class="inline-flex items-center gap-2 rounded-lg bg-amber-50 px-3 py-1 text-xs font-bold text-amber-800 border border-amber-200 mb-2">
          <i class="mdi mdi-account-clock-outline text-base text-amber-700" />
          <span>SHOP APPLICATIONS</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
          คำขอสมัครร้านค้า
        </h1>
        <p class="mt-1 text-sm font-bold text-slate-600">
          ตรวจสอบข้อมูล ร้านค้า และพิจารณาอนุมัติสถานประกอบการที่ยื่นขอเปิดร้านในระบบ
        </p>
      </div>

      <div class="flex flex-wrap items-center gap-3">
        <RouterLink
          to="/admin/shops"
          class="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-xs hover:bg-slate-50 transition"
        >
          <i class="mdi mdi-arrow-left text-base" />
          <span>จัดการร้านค้าทั้งหมด</span>
        </RouterLink>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl border border-slate-300 bg-white px-4 py-2.5 text-sm font-bold text-slate-700 shadow-xs hover:bg-slate-50 transition cursor-pointer"
          :disabled="loading"
          @click="refreshData"
        >
          <i class="mdi mdi-refresh text-lg" :class="{ 'animate-spin': loading }" />
          <span>รีเฟรช</span>
        </button>
      </div>
    </div>

    <!-- PENDING KPI STATS CARD -->
    <section class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div class="rounded-2xl border border-amber-300 bg-gradient-to-br from-amber-50 to-amber-100/50 p-5 shadow-xs">
        <div class="flex items-center justify-between text-amber-800 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">คำขอรอพิจารณา</span>
          <i class="mdi mdi-clock-alert-outline text-2xl text-amber-700" />
        </div>
        <p class="text-3xl font-bold text-amber-900">{{ pagination.totalCount }}</p>
        <p class="mt-1 text-xs text-amber-700 font-bold">รอการตรวจสอบและอนุมัติจากผู้ดูแลระบบ</p>
      </div>

      <div class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs">
        <div class="flex items-center justify-between text-slate-600 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">เกณฑ์การพิจารณา</span>
          <i class="mdi mdi-clipboard-check-outline text-2xl text-slate-500" />
        </div>
        <p class="text-sm font-bold text-slate-800 mt-1 leading-relaxed">
          ตรวจสอบชื่อร้าน หมวดหมู่ ข้อมูลติดต่อ ที่ตั้ง และบัญชีธนาคารให้ถูกต้อง
        </p>
      </div>

      <div class="rounded-2xl border border-slate-300 bg-white p-5 shadow-xs">
        <div class="flex items-center justify-between text-slate-600 mb-2">
          <span class="text-xs font-bold uppercase tracking-wider">ผลการอนุมัติ</span>
          <i class="mdi mdi-store-check-outline text-2xl text-slate-500" />
        </div>
        <p class="text-sm font-bold text-slate-800 mt-1 leading-relaxed">
          เมื่ออนุมัติแล้วร้านค้าจะเปิดใช้งานทันที และสามารถเริ่มลงรายการสินค้าได้
        </p>
      </div>
    </section>

    <!-- SEARCH BAR -->
    <div class="rounded-2xl border border-slate-300 bg-white p-4 shadow-xs">
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="relative w-full sm:w-96">
          <i class="mdi mdi-magnify absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 text-base" />
          <input
            v-model="search"
            type="text"
            placeholder="ค้นหาชื่อร้านค้า หรือเบอร์โทรศัพท์..."
            class="w-full rounded-xl border border-slate-300 bg-white py-2 pl-9 pr-8 text-xs sm:text-sm font-bold text-slate-900 placeholder:text-slate-500 focus:border-teal-600 focus:outline-none focus:ring-1 focus:ring-teal-600"
            @input="onSearchInput"
          />
          <button
            v-if="search"
            type="button"
            class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-500 hover:text-slate-700"
            @click="clearSearch"
          >
            <i class="mdi mdi-close-circle text-sm" />
          </button>
        </div>

        <p class="text-xs text-slate-600 font-bold">
          พบคำขอรอพิจารณา <span class="font-bold text-slate-900">{{ pagination.totalCount }}</span> รายการ
        </p>
      </div>
    </div>

    <!-- MAIN DATA TABLE -->
    <AppDataTable
      :columns="columns"
      :items="shops"
      row-key="shopId"
      :loading="loading"
      empty-message="ไม่มีคำขอสมัครร้านค้าที่รอพิจารณาในขณะนี้"
      :pagination="pagination"
      :variant="'admin'"
      @page-change="changePage"
    >
      <!-- SHOP NAME & CATEGORY -->
      <template #cell-shopName="{ item }">
        <div class="flex items-center gap-3 py-1.5">
          <div class="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-700 font-bold border border-slate-300 overflow-hidden">
            <img
              v-if="asShop(item).coverImageUrl"
              :src="asShop(item).coverImageUrl"
              alt="Cover"
              class="h-full w-full object-cover"
            />
            <i v-else class="mdi mdi-storefront text-2xl text-slate-500" />
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="text-sm font-bold text-slate-900">
                {{ asShop(item).shopName }}
              </span>
              <span
                v-if="asShop(item).categoryName"
                class="rounded-md bg-teal-50 px-2 py-0.5 text-xs font-bold text-teal-800 border border-teal-100"
              >
                {{ asShop(item).categoryName }}
              </span>
            </div>
            <p class="text-xs font-mono text-slate-500 mt-0.5">
              ID: {{ asShop(item).shopId.slice(0, 8) }}
            </p>
          </div>
        </div>
      </template>

      <!-- CONTACT INFO -->
      <template #cell-contact="{ item }">
        <div class="text-xs space-y-0.5">
          <p v-if="asShop(item).phone" class="font-bold text-slate-800 flex items-center gap-1">
            <i class="mdi mdi-phone text-slate-500" />
            <span>{{ asShop(item).phone }}</span>
          </p>
          <p v-if="asShop(item).email" class="text-slate-600 flex items-center gap-1">
            <i class="mdi mdi-email-outline text-slate-500" />
            <span class="truncate max-w-[180px]">{{ asShop(item).email }}</span>
          </p>
          <span v-if="!asShop(item).phone && !asShop(item).email" class="text-slate-500">-</span>
        </div>
      </template>

      <!-- LOCATION -->
      <template #cell-location="{ item }">
        <div class="text-xs">
          <p class="font-bold text-slate-800">
            {{ asShop(item).districtName || 'ไม่ระบุอำเภอ' }}
          </p>
          <p class="text-slate-600">
            {{ asShop(item).subDistrictName || '-' }}
          </p>
        </div>
      </template>

      <!-- STATUS -->
      <template #cell-status>
        <span class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-xs font-bold bg-amber-50 text-amber-800 border border-amber-200">
          <i class="mdi mdi-clock-outline text-xs" />
          <span>รออนุมัติ</span>
        </span>
      </template>

      <!-- ACTIONS COLUMN -->
      <template #cell-actions="{ item }">
        <div class="flex items-center gap-2">
          <!-- QUICK VIEW BUTTON -->
          <button
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-slate-300 bg-white px-2.5 py-1.5 text-xs font-bold text-slate-700 hover:bg-slate-50 transition"
            title="ดูรายละเอียดใบสมัคร"
            @click="openDetail(asShop(item))"
          >
            <i class="mdi mdi-eye-outline text-sm" />
            <span>ดูข้อมูล</span>
          </button>

          <!-- APPROVE BUTTON -->
          <button
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-emerald-300 bg-emerald-600 px-3 py-1.5 text-xs font-bold text-white shadow-2xs hover:bg-emerald-700 transition disabled:opacity-50"
            :disabled="updatingId === asShop(item).shopId"
            title="อนุมัติร้านค้า"
            @click="changeStatus(asShop(item), 'Active')"
          >
            <i class="mdi mdi-check text-sm" />
            <span>อนุมัติ</span>
          </button>

          <!-- REJECT BUTTON -->
          <button
            type="button"
            class="inline-flex items-center gap-1 rounded-lg border border-rose-200 bg-rose-50 px-3 py-1.5 text-xs font-bold text-rose-700 hover:bg-rose-100 transition disabled:opacity-50"
            :disabled="updatingId === asShop(item).shopId"
            title="ไม่อนุมัติร้านค้า"
            @click="changeStatus(asShop(item), 'Rejected')"
          >
            <i class="mdi mdi-close text-sm" />
            <span>ไม่อนุมัติ</span>
          </button>
        </div>
      </template>
    </AppDataTable>

    <!-- SHOP APPLICATION DETAIL MODAL -->
    <Teleport to="body">
      <div
        v-if="selectedShop"
        class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs"
        @click.self="selectedShop = null"
      >
        <div class="relative w-full max-w-2xl max-h-[90vh] overflow-y-auto rounded-2xl border border-slate-300 bg-white p-6 shadow-2xl space-y-6">
          <!-- MODAL HEADER -->
          <div class="flex items-start justify-between border-b border-slate-300 pb-4">
            <div class="flex items-center gap-3">
              <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-slate-100 text-slate-700 font-bold border border-slate-300 overflow-hidden">
                <img
                  v-if="selectedShop.coverImageUrl"
                  :src="selectedShop.coverImageUrl"
                  alt="Cover"
                  class="h-full w-full object-cover"
                />
                <i v-else class="mdi mdi-storefront text-2xl text-slate-500" />
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <h2 class="text-xl font-bold text-slate-900">{{ selectedShop.shopName }}</h2>
                  <span class="inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-bold bg-amber-50 text-amber-800 border border-amber-200">
                    <i class="mdi mdi-clock-outline text-xs" />
                    <span>รออนุมัติ</span>
                  </span>
                </div>
                <p class="text-xs text-slate-600 font-mono mt-0.5">ID: {{ selectedShop.shopId }}</p>
              </div>
            </div>
            <button
              type="button"
              class="rounded-xl p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-700 transition"
              @click="selectedShop = null"
            >
              <i class="mdi mdi-close text-xl" />
            </button>
          </div>

          <!-- MODAL CONTENT -->
          <div class="space-y-4 text-sm text-slate-700">
            <div v-if="selectedShop.description" class="rounded-xl bg-slate-50 p-4 border border-slate-300">
              <p class="text-xs font-bold text-slate-600 uppercase tracking-wider mb-1">คำอธิบายร้านค้า</p>
              <p class="text-slate-800 leading-relaxed font-normal whitespace-pre-line">{{ selectedShop.description }}</p>
            </div>

            <div class="grid grid-cols-2 gap-4">
              <div class="rounded-xl border border-slate-300 p-3.5 bg-white">
                <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">หมวดหมู่ร้านค้า</p>
                <p class="mt-1 font-bold text-slate-900">{{ selectedShop.categoryName || '-' }}</p>
              </div>
              <div class="rounded-xl border border-slate-300 p-3.5 bg-white">
                <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">เวลาเปิด-ปิดทำการ</p>
                <p class="mt-1 font-bold text-slate-900">
                  <span v-if="selectedShop.openingTime && selectedShop.closingTime">
                    {{ selectedShop.openingTime }} - {{ selectedShop.closingTime }} น.
                  </span>
                  <span v-else class="text-slate-500 font-normal">ไม่ระบุเวลาทำการ</span>
                </p>
              </div>
              <div class="rounded-xl border border-slate-300 p-3.5 bg-white">
                <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">เบอร์โทรศัพท์</p>
                <p class="mt-1 font-bold text-slate-900">{{ selectedShop.phone || '-' }}</p>
              </div>
              <div class="rounded-xl border border-slate-300 p-3.5 bg-white">
                <p class="text-xs font-bold text-slate-500 uppercase tracking-wider">อีเมลติดต่อ</p>
                <p class="mt-1 font-bold text-slate-900 break-all">{{ selectedShop.email || '-' }}</p>
              </div>
            </div>

            <!-- LOCATION -->
            <div class="rounded-xl border border-slate-300 p-3.5 bg-white">
              <p class="text-xs font-bold text-slate-500 uppercase tracking-wider mb-1">ที่ตั้งร้านค้า</p>
              <p class="text-slate-900 font-bold">
                {{ selectedShop.address || 'ไม่ระบุที่อยู่' }}
              </p>
              <p class="text-xs text-slate-600 mt-0.5">
                อำเภอ: {{ selectedShop.districtName || '-' }} | ตำบล: {{ selectedShop.subDistrictName || '-' }}
              </p>
            </div>

            <!-- BANK & PROMPTPAY -->
            <div class="rounded-xl border border-slate-300 p-3.5 bg-slate-50/60">
              <p class="text-xs font-bold text-slate-600 uppercase tracking-wider mb-2">ข้อมูลบัญชีธนาคารสำหรับรับเงิน</p>
              <div class="grid grid-cols-2 gap-3 text-xs">
                <div>
                  <span class="text-slate-500">ธนาคาร:</span>
                  <span class="font-bold text-slate-800 ml-1.5">{{ selectedShop.bankName || '-' }}</span>
                </div>
                <div>
                  <span class="text-slate-500">ชื่อบัญชี:</span>
                  <span class="font-bold text-slate-800 ml-1.5">{{ selectedShop.bankAccountName || '-' }}</span>
                </div>
                <div>
                  <span class="text-slate-500">เลขบัญชี:</span>
                  <span class="font-mono font-bold text-slate-800 ml-1.5">{{ selectedShop.bankAccountNumber || '-' }}</span>
                </div>
                <div>
                  <span class="text-slate-500">พร้อมเพย์:</span>
                  <span class="font-mono font-bold text-slate-800 ml-1.5">{{ selectedShop.promptPay || '-' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- MODAL ACTIONS -->
          <div class="flex items-center justify-between border-t border-slate-300 pt-4">
            <button
              type="button"
              class="rounded-xl border border-slate-300 px-4 py-2 text-sm font-bold text-slate-700 hover:bg-slate-50 transition"
              @click="selectedShop = null"
            >
              ปิดหน้าต่าง
            </button>

            <div class="flex items-center gap-3">
              <button
                type="button"
                class="rounded-xl border border-rose-300 bg-rose-50 px-4 py-2 text-sm font-bold text-rose-700 hover:bg-rose-100 transition inline-flex items-center gap-1.5"
                @click="rejectFromModal(selectedShop)"
              >
                <i class="mdi mdi-close" />
                <span>ไม่อนุมัติ</span>
              </button>

              <button
                type="button"
                class="rounded-xl bg-emerald-600 px-5 py-2 text-sm font-bold text-white hover:bg-emerald-700 transition inline-flex items-center gap-1.5"
                @click="approveFromModal(selectedShop)"
              >
                <i class="mdi mdi-check" />
                <span>อนุมัติคำขอเปิดร้าน</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getAdminShops, updateShopStatus } from '../api/adminShopApi'
import type { Shop } from '@/features/shops/shared/types/shop'
import AppDataTable, {
  type DataTableColumn,
  type DataTablePagination,
} from '@/components/common/AppDataTable.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import type { PagedResult } from '@/shared/interface/PagedResult'

const shops = ref<Shop[]>([])
const search = ref('')
const loading = ref(true)
const updatingId = ref<string | null>(null)
const selectedShop = ref<Shop | null>(null)

const pagination = ref<DataTablePagination>({
  page: 1,
  pageSize: 15,
  totalCount: 0,
  totalPages: 0,
})

const swal = useSwal()

const columns: DataTableColumn[] = [
  { key: 'shopName', label: 'ร้านค้าที่ยื่นขอ' },
  { key: 'contact', label: 'ข้อมูลติดต่อ' },
  { key: 'location', label: 'พื้นที่ตั้ง' },
  { key: 'status', label: 'สถานะ' },
  { key: 'actions', label: 'การพิจารณา' },
]

function asShop(item: object): Shop {
  return item as Shop
}

async function load() {
  loading.value = true
  try {
    const result: PagedResult<Shop> = await getAdminShops({
      search: search.value.trim() || undefined,
      status: 'PendingApproval',
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
    await swal.error('โหลดคำขอสมัครร้านค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

function onSearchInput() {
  pagination.value.page = 1
  load()
}

function clearSearch() {
  search.value = ''
  pagination.value.page = 1
  load()
}

function changePage(page: number) {
  pagination.value.page = page
  load()
}

function refreshData() {
  load()
}

function openDetail(shop: Shop) {
  selectedShop.value = shop
}

async function changeStatus(shop: Shop, status: 'Active' | 'Rejected') {
  const isApproving = status === 'Active'
  const actionText = isApproving ? 'อนุมัติคำขอเปิดร้านค้า' : 'ไม่อนุมัติคำขอเปิดร้านค้า'
  const result = await swal.confirm(`ยืนยันการ${actionText}?`, shop.shopName)
  if (!result.isConfirmed) return

  updatingId.value = shop.shopId
  try {
    await updateShopStatus(shop.shopId, status)
    await swal.success(isApproving ? 'อนุมัติร้านค้าเรียบร้อยแล้ว' : 'ปฏิเสธคำขอเปิดร้านเรียบร้อยแล้ว')
    await load()
  } catch (error) {
    await swal.error('อัปเดตสถานะไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

async function approveFromModal(shop: Shop) {
  selectedShop.value = null
  await changeStatus(shop, 'Active')
}

async function rejectFromModal(shop: Shop) {
  selectedShop.value = null
  await changeStatus(shop, 'Rejected')
}

onMounted(load)
</script>
