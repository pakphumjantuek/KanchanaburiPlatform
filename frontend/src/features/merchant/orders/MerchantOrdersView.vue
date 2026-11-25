<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import http from '@/shared/api/http'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { formatDate } from '@/shared/utils/formatDate'
import AppDateRangeFilter, { type DatePreset } from '@/shared/components/AppDateRangeFilter.vue'
import MerchantOrderCard, { type OrderItem } from './components/MerchantOrderCard.vue'

const orders = ref<OrderItem[]>([])
const allOrdersForStats = ref<OrderItem[]>([])
const loading = ref(true)
const fetching = ref(false)
const updatingId = ref<string | null>(null)
const selectedFilter = ref<string>('all')
const searchNumber = ref<string>('')
const viewMode = ref<'cards' | 'table'>('cards')
const showDateFilter = ref(false)

// Date Filter States
const datePreset = ref<DatePreset>('all')
const startDate = ref<string>('')
const endDate = ref<string>('')

const swal = useSwal()

// Load API with Backend Filtering
async function load(isInitial = false) {
  if (isInitial) {
    loading.value = true
  } else {
    fetching.value = true
  }
  try {
    const params: Record<string, any> = {}

    if (selectedFilter.value !== 'all') {
      params.status = selectedFilter.value
    }
    if (searchNumber.value.trim()) {
      params.search = searchNumber.value.trim()
    }

    if (datePreset.value === 'today') {
      const todayStr = new Date().toISOString().slice(0, 10)
      params.startDate = todayStr
      params.endDate = todayStr
    } else if (datePreset.value === '7days') {
      const past7 = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
      params.startDate = past7
    } else if (datePreset.value === '30days') {
      const past30 = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
      params.startDate = past30
    } else if (datePreset.value === 'custom') {
      if (startDate.value) params.startDate = startDate.value
      if (endDate.value) params.endDate = endDate.value
    }

    const { data } = await http.get<OrderItem[]>('/orders/shop/mine', { params })
    orders.value = data
    if (isInitial || !allOrdersForStats.value.length) {
      allOrdersForStats.value = data
    }
  } catch (error) {
    await swal.error('โหลดออเดอร์ไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
    fetching.value = false
  }
}

// Refetch smoothly on filter change
watch([selectedFilter, searchNumber, datePreset, startDate, endDate], () => {
  void load(false)
})

const totalPendingCount = computed(
  () => allOrdersForStats.value.filter((o) => o.orderStatus === 'Pending').length,
)
const totalConfirmedCount = computed(
  () => allOrdersForStats.value.filter((o) => o.orderStatus === 'Confirmed').length,
)
const totalProcessingCount = computed(
  () => allOrdersForStats.value.filter((o) => o.orderStatus === 'Processing').length,
)
const totalShippedCount = computed(
  () =>
    allOrdersForStats.value.filter(
      (o) => o.orderStatus === 'Shipped' || o.orderStatus === 'Completed',
    ).length,
)

const tabs = computed(() => [
  { id: 'all', label: 'ทั้งหมด', count: allOrdersForStats.value.length, badgeClass: 'bg-slate-900 text-white' },
  { id: 'Pending', label: 'รอยืนยัน', count: totalPendingCount.value, badgeClass: 'bg-amber-500 text-white' },
  { id: 'Confirmed', label: 'ยืนยันแล้ว', count: totalConfirmedCount.value, badgeClass: 'bg-blue-600 text-white' },
  { id: 'Processing', label: 'เตรียมจัดส่ง', count: totalProcessingCount.value, badgeClass: 'bg-teal-600 text-white' },
  { id: 'Shipped', label: 'จัดส่งแล้ว', count: totalShippedCount.value, badgeClass: 'bg-emerald-700 text-white' },
])

function clearDateFilter() {
  datePreset.value = 'all'
  startDate.value = ''
  endDate.value = ''
}

async function handleUpdateStatus({ order, status }: { order: OrderItem; status: string }) {
  if (status === 'Cancelled') {
    const confirmation = await swal.confirm(
      'ยืนยันการยกเลิกออเดอร์?',
      'การยกเลิกจะไม่สามารถเปลี่ยนกลับเป็นสถานะเดิมได้',
    )

    if (!confirmation.isConfirmed) {
      return
    }
  }

  updatingId.value = order.orderId
  try {
    await http.patch(`/orders/${order.orderId}/status`, { status })
    order.orderStatus = status
    const statusTitle =
      status === 'Confirmed'
        ? 'ยืนยันออเดอร์แล้ว'
        : status === 'Processing'
          ? 'กำลังเตรียมจัดส่ง'
          : 'ยกเลิกออเดอร์แล้ว'
    await swal.success('อัปเดตสถานะออเดอร์แล้ว', `เปลี่ยนสถานะเป็น "${statusTitle}" เรียบร้อยแล้ว`)
  } catch (error) {
    await swal.error('อัปเดตไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

async function handleShipOrder({
  order,
  provider,
  trackingNumber,
}: {
  order: OrderItem
  provider: string
  trackingNumber: string
}) {
  if (!provider || !trackingNumber) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณาระบุบริษัทขนส่งและเลขพัสดุ')
    return
  }

  updatingId.value = order.orderId
  try {
    await http.put(`/orders/${order.orderId}/shipment`, {
      shippingProvider: provider,
      trackingNumber: trackingNumber,
    })
    order.orderStatus = 'Shipped'
    order.shippingProvider = provider
    order.trackingNumber = trackingNumber
    await swal.success('บันทึกการจัดส่งแล้ว', 'ระบบอัปเดตเลขพัสดุและแจ้งลูกค้าเรียบร้อยแล้ว')
  } catch (error) {
    await swal.error('บันทึกการจัดส่งไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    updatingId.value = null
  }
}

const apiOrigin = (import.meta.env.VITE_API_URL ?? 'https://localhost:7289/api').replace(
  /\/api$/,
  '',
)
function imageUrl(url?: string | null) {
  if (!url) return ''
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}

onMounted(() => void load(true))
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-5 pb-16 text-slate-900">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-4">
      <div class="h-20 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      <div class="h-14 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      <div v-for="i in 3" :key="i" class="h-40 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
    </div>

    <template v-else>
      <!-- 1. Header Bar (Bold, High Contrast, Crystal Clear) -->
      <div
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs"
      >
        <div class="flex items-center gap-3.5">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-700 text-white shadow-sm shrink-0"
          >
            <i class="mdi mdi-clipboard-text-clock-outline text-2xl text-white"></i>
          </div>
          <div>
            <div class="flex items-center gap-2.5">
              <h1 class="text-2xl font-black text-slate-950 tracking-tight">
                จัดการคำสั่งซื้อ
              </h1>
              <span class="rounded-xl bg-emerald-100 text-emerald-950 border border-emerald-300 px-3 py-0.5 text-xs font-black">
                {{ orders.length }} รายการ
              </span>
            </div>
            <p class="text-xs sm:text-sm text-slate-700 font-bold mt-0.5">
              ยืนยันออเดอร์ ตรวจสอบการชำระเงิน และบันทึกเลขพัสดุ
            </p>
          </div>
        </div>

        <!-- Controls: Refresh + Date Filter + View Switcher -->
        <div class="flex items-center gap-2.5 flex-wrap">
          <!-- Refresh -->
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-xl border-2 border-slate-300 bg-white hover:bg-slate-100 px-3.5 py-2 text-xs font-black text-slate-900 transition cursor-pointer select-none shadow-2xs"
            :disabled="fetching"
            title="รีเฟรชข้อมูลล่าสุด"
            @click="() => void load(false)"
          >
            <i :class="['mdi text-base text-emerald-700', fetching ? 'mdi-loading animate-spin' : 'mdi-refresh']"></i>
            <span>{{ fetching ? 'กำลังโหลด...' : 'รีเฟรช' }}</span>
          </button>

          <!-- Toggle Date Filter Button -->
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-xl border-2 px-3.5 py-2 text-xs font-black transition cursor-pointer select-none shadow-2xs"
            :class="
              showDateFilter || datePreset !== 'all'
                ? 'border-emerald-700 bg-emerald-100 text-emerald-950'
                : 'border-slate-300 bg-white text-slate-900 hover:bg-slate-100'
            "
            @click="showDateFilter = !showDateFilter"
          >
            <i class="mdi mdi-calendar-range text-base text-emerald-800"></i>
            <span>{{ datePreset === 'all' ? 'กรองวันที่' : 'ช่วงวันที่เลือกไว้' }}</span>
          </button>

          <!-- View Mode Toggle -->
          <div class="flex items-center rounded-xl bg-slate-200 p-1 border border-slate-300">
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-black transition cursor-pointer"
              :class="
                viewMode === 'cards'
                  ? 'bg-white text-slate-950 shadow-xs'
                  : 'text-slate-700 hover:text-slate-950'
              "
              @click="viewMode = 'cards'"
              title="มุมมองแบบการ์ด"
            >
              <i class="mdi mdi-view-agenda-outline text-sm"></i>
              <span class="hidden sm:inline">การ์ด</span>
            </button>
            <button
              type="button"
              class="flex items-center gap-1.5 rounded-lg px-3 py-1.5 text-xs font-black transition cursor-pointer"
              :class="
                viewMode === 'table'
                  ? 'bg-white text-slate-950 shadow-xs'
                  : 'text-slate-700 hover:text-slate-950'
              "
              @click="viewMode = 'table'"
              title="มุมมองแบบตาราง"
            >
              <i class="mdi mdi-table text-sm"></i>
              <span class="hidden sm:inline">ตาราง</span>
            </button>
          </div>
        </div>
      </div>

      <!-- 2. High Contrast Status Tab Bar (Vivid, Clear Numbers, Easy to Read) -->
      <div class="rounded-2xl border-2 border-slate-200 bg-slate-100/90 p-2 shadow-xs flex flex-col md:flex-row md:items-center justify-between gap-3">
        <!-- Status Tabs -->
        <div class="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0 scrollbar-none flex-1">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            type="button"
            class="flex items-center gap-2 rounded-xl px-4 py-2 text-xs font-black transition shrink-0 cursor-pointer select-none"
            :class="
              selectedFilter === tab.id
                ? 'bg-emerald-800 text-white shadow-sm'
                : 'bg-white text-slate-900 border border-slate-200 hover:bg-slate-50'
            "
            @click="selectedFilter = tab.id"
          >
            <span>{{ tab.label }}</span>
            <span
              class="rounded-full px-2.5 py-0.5 text-[11px] font-black"
              :class="selectedFilter === tab.id ? 'bg-white/25 text-white' : tab.badgeClass"
            >
              {{ tab.count }}
            </span>
          </button>
        </div>

        <!-- Quick Search Order Number Input -->
        <div class="relative min-w-[240px]">
          <input
            v-model="searchNumber"
            type="text"
            placeholder="ค้นหาเลขออเดอร์..."
            class="w-full rounded-xl border-2 border-slate-300 bg-white pl-9 pr-8 py-2 text-xs font-bold text-slate-950 placeholder:text-slate-500 focus:border-emerald-700 transition"
          />
          <i class="mdi mdi-magnify absolute left-3 top-1/2 -translate-y-1/2 text-slate-500 text-base"></i>
          <button
            v-if="searchNumber"
            type="button"
            class="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-800 cursor-pointer"
            @click="searchNumber = ''"
          >
            <i class="mdi mdi-close-circle text-base"></i>
          </button>
        </div>
      </div>

      <!-- Collapsible Date Filter Bar -->
      <div
        v-if="showDateFilter"
        class="rounded-2xl border-2 border-slate-200 bg-white p-4 shadow-xs"
      >
        <AppDateRangeFilter
          v-model:preset="datePreset"
          v-model:start-date="startDate"
          v-model:end-date="endDate"
          @clear="clearDateFilter"
        />
      </div>

      <!-- 3. Empty State: No Orders in Shop -->
      <div
        v-if="allOrdersForStats.length === 0"
        class="my-8 flex flex-col items-center justify-center rounded-2xl border-2 border-dashed border-slate-300 bg-white py-16 px-6 text-center shadow-xs"
      >
        <div class="flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 mb-4">
          <i class="mdi mdi-clipboard-text-off-outline text-3xl"></i>
        </div>
        <h2 class="text-xl font-black text-slate-950">ยังไม่มีรายการคำสั่งซื้อเข้ามาในร้าน</h2>
        <p class="mt-1 max-w-sm text-xs sm:text-sm text-slate-700 font-bold">
          เมื่อลูกค้าสั่งซื้อสินค้าจากร้านค้าของคุณ ข้อมูลคำสั่งซื้อจะปรากฏที่หน้านี้โดยอัตโนมัติ
        </p>
      </div>

      <!-- 4. Empty State: Filter / Search returns 0 -->
      <div
        v-else-if="orders.length === 0"
        class="rounded-2xl border-2 border-slate-200 bg-white p-12 text-center text-slate-700 shadow-xs space-y-3"
      >
        <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-200 text-slate-800">
          <i class="mdi mdi-text-box-search-outline text-3xl"></i>
        </div>
        <div>
          <p class="font-black text-slate-950 text-base">ไม่พบคำสั่งซื้อในตัวกรองนี้</p>
          <p class="text-xs text-slate-600 font-bold mt-1">ลองเลือกสถานะอื่น หรือล้างคำค้นหา</p>
        </div>
        <button
          type="button"
          class="inline-flex items-center gap-1.5 rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white px-4 py-2 text-xs font-bold transition shadow-xs cursor-pointer select-none"
          @click="
            () => {
              selectedFilter = 'all'
              searchNumber = ''
              clearDateFilter()
            }
          "
        >
          <i class="mdi mdi-filter-remove-outline text-sm"></i>
          <span>ล้างตัวกรอง</span>
        </button>
      </div>

      <!-- 5. Orders View: CARDS MODE -->
      <div v-else-if="viewMode === 'cards'" class="space-y-4">
        <MerchantOrderCard
          v-for="order in orders"
          :key="order.orderId"
          :order="order"
          :updating-id="updatingId"
          @update-status="handleUpdateStatus"
          @ship-order="handleShipOrder"
        />
      </div>

      <!-- 6. Orders View: TABLE MODE (High Contrast, Clear Reading) -->
      <div v-else-if="viewMode === 'table'" class="rounded-2xl border-2 border-slate-200 bg-white shadow-sm overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-sm">
            <thead>
              <tr class="border-b-2 border-slate-200 bg-slate-100 text-xs font-black text-slate-900 uppercase tracking-wider">
                <th class="py-4 px-5">เลขออเดอร์ & วันที่</th>
                <th class="py-4 px-5">สินค้า</th>
                <th class="py-4 px-5">ยอดรวมสุทธิ</th>
                <th class="py-4 px-5">การชำระเงิน</th>
                <th class="py-4 px-5">สถานะออเดอร์</th>
                <th class="py-4 px-5 text-right">ดำเนินการ</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-200 font-bold text-slate-900">
              <tr
                v-for="order in orders"
                :key="order.orderId"
                class="hover:bg-slate-50 transition"
              >
                <!-- Order # and Date -->
                <td class="py-4 px-5">
                  <div class="font-mono text-sm font-black text-slate-950">
                    #{{ order.orderNumber }}
                  </div>
                  <div class="text-xs text-slate-600 font-bold mt-0.5">
                    {{ formatDate(order.createdAt, { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' }) }}
                  </div>
                </td>

                <!-- Products Summary -->
                <td class="py-4 px-5 max-w-[260px]">
                  <div class="flex items-center gap-3">
                    <div class="h-11 w-11 rounded-xl bg-white border border-slate-300 overflow-hidden shrink-0 flex items-center justify-center shadow-2xs">
                      <img
                        v-if="order.items[0]?.imageUrl"
                        :src="imageUrl(order.items[0].imageUrl)"
                        :alt="order.items[0].productName"
                        class="h-full w-full object-cover"
                      />
                      <i v-else class="mdi mdi-package-variant-closed text-slate-400 text-lg"></i>
                    </div>
                    <div class="min-w-0">
                      <p class="text-xs sm:text-sm font-black text-slate-950 truncate">
                        {{ order.items[0]?.productName }}
                      </p>
                      <p class="text-xs text-slate-600 font-bold">
                        {{ order.items.reduce((t, i) => t + i.quantity, 0) }} ชิ้น
                        <span v-if="order.items.length > 1" class="text-slate-800">(+อีก {{ order.items.length - 1 }} รายการ)</span>
                      </p>
                    </div>
                  </div>
                </td>

                <!-- Total Amount -->
                <td class="py-4 px-5">
                  <span class="font-black text-slate-950 text-base">
                    ฿{{ Number(order.totalAmount).toLocaleString('th-TH') }}
                  </span>
                </td>

                <!-- Payment Status (Solid Vivid Pill) -->
                <td class="py-4 px-5">
                  <span
                    v-if="order.paymentStatus === 'Paid'"
                    class="inline-flex items-center gap-1 rounded-xl bg-emerald-600 text-white px-2.5 py-1 text-xs font-bold"
                  >
                    <i class="mdi mdi-check-circle text-xs"></i>
                    <span>ชำระเงินแล้ว</span>
                  </span>
                  <span
                    v-else-if="order.paymentStatus === 'PendingVerification'"
                    class="inline-flex items-center gap-1 rounded-xl bg-amber-500 text-white px-2.5 py-1 text-xs font-bold"
                  >
                    <span>รอตรวจสลิป</span>
                  </span>
                  <span
                    v-else
                    class="inline-flex items-center gap-1 rounded-xl bg-rose-600 text-white px-2.5 py-1 text-xs font-bold"
                  >
                    <span>รอชำระเงิน</span>
                  </span>
                </td>

                <!-- Order Status -->
                <td class="py-4 px-5">
                  <span
                    class="inline-flex items-center gap-1 rounded-xl px-2.5 py-1 text-xs font-bold border-2"
                    :class="{
                      'bg-amber-100 text-amber-950 border-amber-400': order.orderStatus === 'Pending',
                      'bg-blue-100 text-blue-950 border-blue-400': order.orderStatus === 'Confirmed',
                      'bg-teal-100 text-teal-950 border-teal-400': order.orderStatus === 'Processing',
                      'bg-emerald-100 text-emerald-950 border-emerald-400': order.orderStatus === 'Shipped' || order.orderStatus === 'Completed',
                      'bg-slate-200 text-slate-800 border-slate-300': order.orderStatus === 'Cancelled',
                    }"
                  >
                    {{
                      order.orderStatus === 'Pending'
                        ? 'รอยืนยัน'
                        : order.orderStatus === 'Confirmed'
                          ? 'ยืนยันแล้ว'
                          : order.orderStatus === 'Processing'
                            ? 'เตรียมจัดส่ง'
                            : order.orderStatus === 'Shipped' || order.orderStatus === 'Completed'
                              ? 'จัดส่งแล้ว'
                              : 'ยกเลิกแล้ว'
                    }}
                  </span>
                </td>

                <!-- Actions -->
                <td class="py-4 px-5 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      v-if="order.orderStatus === 'Pending'"
                      type="button"
                      class="rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white px-3.5 py-1.5 text-xs font-bold transition shadow-2xs cursor-pointer"
                      :disabled="updatingId === order.orderId"
                      @click="handleUpdateStatus({ order, status: 'Confirmed' })"
                    >
                      ยืนยันออเดอร์
                    </button>
                    <button
                      v-else-if="order.orderStatus === 'Confirmed'"
                      type="button"
                      class="rounded-xl bg-teal-700 hover:bg-teal-800 text-white px-3.5 py-1.5 text-xs font-bold transition shadow-2xs cursor-pointer"
                      :disabled="updatingId === order.orderId"
                      @click="handleUpdateStatus({ order, status: 'Processing' })"
                    >
                      เริ่มเตรียมส่ง
                    </button>
                    <button
                      v-else-if="order.orderStatus === 'Processing' && order.paymentStatus === 'Paid'"
                      type="button"
                      class="rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white px-3.5 py-1.5 text-xs font-bold transition shadow-2xs cursor-pointer"
                      @click="viewMode = 'cards'"
                    >
                      กรอกเลขพัสดุ
                    </button>
                    <span
                      v-else-if="order.orderStatus === 'Shipped' || order.orderStatus === 'Completed'"
                      class="text-xs font-mono font-black text-emerald-950 bg-emerald-100 border border-emerald-300 px-2 py-0.5 rounded-lg"
                    >
                      {{ order.trackingNumber || 'จัดส่งแล้ว' }}
                    </span>
                    <span v-else class="text-xs text-slate-500 font-bold">
                      -
                    </span>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
button,
a,
input {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active {
  outline: none !important;
  box-shadow: none !important;
}

.scrollbar-none::-webkit-scrollbar {
  display: none;
}
.scrollbar-none {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
