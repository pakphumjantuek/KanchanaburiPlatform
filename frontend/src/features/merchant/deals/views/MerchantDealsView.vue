<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { createDeal, getProductDeals, stopDeal } from '../api/productDealApi'
import { getMyShop } from '@/features/merchant/api/shopApi'
import { getProducts, type Product } from '@/features/shops/api'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { imageUrl } from '@/shared/utils/imageUrl'
import type { ProductDeal } from '../interface/type'

const route = useRoute()
const swal = useSwal()
const deals = ref<ProductDeal[]>([])
const products = ref<Product[]>([])
const loading = ref(true)
const saving = ref(false)
const showForm = ref(false)

const currentProduct = computed(() => products.value[0] ?? null)
const productName = computed(() => currentProduct.value?.productName ?? 'กำลังโหลดชื่อสินค้า...')

const form = ref({
  productId: '',
  dealType: 'FlashDeal',
  discountType: 'FixedAmount',
  discountValue: 0,
  totalQuantity: 20,
  startsAt: '',
  endsAt: '',
})

// KPI Metrics
const totalDealsCount = computed(() => deals.value.length)
const activeDealsCount = computed(() => deals.value.filter((d) => getEffectiveStatus(d) === 'Active').length)
const totalQuota = computed(() => deals.value.reduce((acc, d) => acc + (d.totalQuantity || 0), 0))
const totalUsedQuota = computed(() => deals.value.reduce((acc, d) => acc + (d.usedQuantity || 0), 0))

// Live Calculation in Form
const calculatedDiscountPrice = computed(() => {
  if (!currentProduct.value) return 0
  const original = Number(currentProduct.value.price) || 0
  const val = Number(form.value.discountValue) || 0
  if (form.value.discountType === 'Percent') {
    const discounted = original - original * (val / 100)
    return Math.max(0, Math.round(discounted))
  } else {
    return Math.max(0, original - val)
  }
})

const calculatedSavings = computed(() => {
  if (!currentProduct.value) return 0
  const original = Number(currentProduct.value.price) || 0
  return Math.max(0, original - calculatedDiscountPrice.value)
})

function getDealDiscountedPrice(deal: ProductDeal) {
  if (!currentProduct.value) return null
  const original = Number(currentProduct.value.price) || 0
  if (deal.discountType === 'Percent') {
    const discounted = original - original * (deal.discountValue / 100)
    return Math.max(0, Math.round(discounted))
  } else {
    return Math.max(0, original - deal.discountValue)
  }
}

function toDatetimeLocal(date: Date) {
  const pad = (n: number) => n.toString().padStart(2, '0')
  const y = date.getFullYear()
  const m = pad(date.getMonth() + 1)
  const d = pad(date.getDate())
  const h = pad(date.getHours())
  const min = pad(date.getMinutes())
  return `${y}-${m}-${d}T${h}:${min}`
}

function applyPreset(hours: number) {
  const now = new Date()
  const end = new Date(now.getTime() + hours * 60 * 60 * 1000)
  form.value.startsAt = toDatetimeLocal(now)
  form.value.endsAt = toDatetimeLocal(end)
}

function parseIsoDate(value: string | undefined): Date | null {
  if (!value) return null
  const isoValue = value.replace(' ', 'T')
  const hasTimezone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(isoValue)
  return new Date(hasTimezone ? isoValue : `${isoValue}Z`)
}

function isDealExpired(deal: ProductDeal) {
  const d = parseIsoDate(deal.endsAt)
  if (!d) return false
  return Date.now() >= d.getTime()
}

function isDealStarted(deal: ProductDeal) {
  const d = parseIsoDate(deal.startsAt)
  if (!d) return true
  return Date.now() >= d.getTime()
}

function getEffectiveStatus(deal: ProductDeal): 'Active' | 'Stopped' | 'Expired' | 'Upcoming' {
  if (deal.status === 'Stopped') return 'Stopped'
  if (isDealExpired(deal)) return 'Expired'
  if (!isDealStarted(deal)) return 'Upcoming'
  return 'Active'
}

function getRemainingTimeText(endsAt: string | undefined) {
  const d = parseIsoDate(endsAt)
  if (!d) return ''
  const diff = d.getTime() - Date.now()
  if (diff <= 0) return 'หมดระยะเวลาแคมเปญแล้ว'
  const days = Math.floor(diff / (1000 * 60 * 60 * 24))
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
  const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  if (days > 0) return `เหลือ ${days} วัน ${hours} ชม.`
  if (hours > 0) return `เหลือ ${hours} ชม. ${mins} นาที`
  return `เหลือ ${mins} นาที`
}

function openCreateForm() {
  showForm.value = true
  if (!form.value.startsAt) {
    applyPreset(72) // default 3 days
  }
}

async function load() {
  loading.value = true
  try {
    deals.value = await getProductDeals(String(route.params.id))
  } catch (error) {
    console.error(error)
  } finally {
    loading.value = false
  }
}

async function handleStopDeal(id: string) {
  const result = await swal.confirm(
    'ยืนยันหยุดดีลนี้หรือไม่?',
    'เมื่อหยุดดีลแล้ว ลูกค้าจะไม่สามารถใช้ส่วนลดนี้ได้อีก',
  )
  if (!result.isConfirmed) return
  try {
    await stopDeal(id)
    await swal.success('หยุดดีลเรียบร้อยแล้ว')
    await load()
  } catch (error) {
    await swal.error('ไม่สามารถหยุดดีลได้', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function save() {
  if (!form.value.productId || !form.value.startsAt || !form.value.endsAt) {
    await swal.warning('ข้อมูลไม่ครบถ้วน', 'กรุณาระบุช่วงเวลาเริ่มต้นและสิ้นสุดของดีล')
    return
  }
  if (form.value.discountValue <= 0) {
    await swal.warning('ข้อมูลไม่ถูกต้อง', 'กรุณาระบุมูลค่าส่วนลดที่มากกว่า 0')
    return
  }
  if (form.value.totalQuantity <= 0) {
    await swal.warning('ข้อมูลไม่ถูกต้อง', 'จำนวนสิทธิ์ต้องมากกว่า 0')
    return
  }

  saving.value = true
  try {
    await createDeal(form.value.productId, {
      ...form.value,
      startsAt: new Date(form.value.startsAt).toISOString(),
      endsAt: new Date(form.value.endsAt).toISOString(),
    })
    await swal.success('สร้างดีลโปรโมชันเรียบร้อยแล้ว')
    showForm.value = false
    await load()
  } catch (error) {
    await swal.error('สร้างดีลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

function label(d: ProductDeal) {
  return d.dealType === 'FlashDeal' ? 'Flash Deal (โปรโมชันพิเศษ)' : 'Local Deal (โปรเด็ดชุมชน)'
}

function formatThaiDate(value: string) {
  if (!value) return '-'
  try {
    const isoValue = value.replace(' ', 'T')
    const hasTimezone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(isoValue)
    return new Intl.DateTimeFormat('th-TH', { dateStyle: 'medium', timeStyle: 'short' }).format(
      new Date(hasTimezone ? isoValue : `${isoValue}Z`),
    )
  } catch {
    return value
  }
}

onMounted(async () => {
  form.value.productId = String(route.params.id)
  await load()
  try {
    const shop = await getMyShop()
    const allProducts = await getProducts()
    products.value = allProducts.filter(
      (product) => product.shopId === shop.shopId && product.productId === String(route.params.id),
    )
  } catch (error) {
    console.error(error)
  }
})
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 pb-16 text-slate-800">
    <!-- 1. Header Banner -->
    <div
      class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs"
    >
      <div class="flex items-center gap-4">
        <div
          class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-600 text-white shadow-xs shrink-0"
        >
          <i class="mdi mdi-sale text-2xl text-white"></i>
        </div>
        <div>
          <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-1">
            <i class="mdi mdi-ticket-percent-outline text-xs text-emerald-600"></i>
            <span>ศูนย์โปรโมชัน • การตลาดและส่วนลด</span>
          </div>
          <h1 class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
            จัดการโปรโมชันและดีลสินค้า
          </h1>
          <p class="text-xs sm:text-sm text-slate-500 font-medium mt-0.5">
            กำหนดส่วนลดพิเศษ ควบคุมจำนวนสิทธิ์ และตั้งเวลาแคมเปญโปรโมชัน
          </p>
        </div>
      </div>

      <div class="flex items-center gap-2.5 self-start sm:self-auto flex-wrap">
        <RouterLink
          to="/my-shop/products"
          class="inline-flex items-center gap-1.5 rounded-full border-2 border-slate-200 bg-slate-50 hover:bg-slate-100 px-4 py-2 text-xs sm:text-sm font-bold text-slate-700 transition cursor-pointer select-none outline-none focus:outline-none"
        >
          <i class="mdi mdi-arrow-left text-base text-slate-500"></i>
          <span>กลับหน้ารายการสินค้า</span>
        </RouterLink>

        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-full bg-emerald-600 hover:bg-emerald-700 px-5 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 cursor-pointer border-2 border-emerald-600 outline-none focus:outline-none"
          @click="showForm ? (showForm = false) : openCreateForm()"
        >
          <i :class="['mdi text-lg text-white', showForm ? 'mdi-close' : 'mdi-plus-circle']"></i>
          <span>{{ showForm ? 'ปิดแบบฟอร์ม' : 'สร้างดีลใหม่' }}</span>
        </button>
      </div>
    </div>

    <!-- 2. Product Info Card -->
    <div
      v-if="currentProduct"
      class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4"
    >
      <div class="flex items-center gap-4 min-w-0">
        <div
          class="h-16 w-16 rounded-xl bg-slate-100 overflow-hidden shrink-0 border border-slate-200 flex items-center justify-center"
        >
          <img
            v-if="currentProduct.imageUrl"
            :src="imageUrl(currentProduct.imageUrl)"
            :alt="currentProduct.productName"
            class="h-full w-full object-cover"
          />
          <i v-else class="mdi mdi-package-variant-closed text-2xl text-slate-400"></i>
        </div>
        <div class="min-w-0">
          <div class="flex items-center gap-2">
            <span
              v-if="(currentProduct as any).categoryName"
              class="rounded-md bg-emerald-50 text-emerald-700 border border-emerald-100 px-2 py-0.5 text-[11px] font-semibold"
            >
              {{ (currentProduct as any).categoryName }}
            </span>
            <span
              v-if="currentProduct.quantity <= 5"
              class="rounded-md bg-amber-50 text-amber-700 border border-amber-200 px-2 py-0.5 text-[11px] font-semibold"
            >
              สต็อกเหลือน้อย: {{ currentProduct.quantity }} ชิ้น
            </span>
          </div>
          <h2 class="text-base sm:text-lg font-bold text-slate-900 truncate mt-1">
            {{ currentProduct.productName }}
          </h2>
          <div class="flex items-center gap-3 text-xs text-slate-500 mt-0.5">
            <span>
              ราคาขายปกติ:
              <strong class="text-slate-800 font-bold"
                >฿{{ Number(currentProduct.price).toLocaleString() }}</strong
              >
            </span>
            <span>•</span>
            <span>คงเหลือในคลัง: {{ currentProduct.quantity }} ชิ้น</span>
          </div>
        </div>
      </div>

      <div class="flex items-center gap-2 shrink-0">
        <RouterLink
          :to="`/shops/${currentProduct.shopId}/products/${currentProduct.productId}`"
          target="_blank"
          class="inline-flex items-center gap-1.5 rounded-full border-2 border-slate-200 bg-slate-50 px-4 py-2 text-xs font-bold text-slate-700 hover:bg-slate-100 transition outline-none focus:outline-none"
        >
          <span>ดูสินค้านี้บนหน้าร้าน</span>
          <i class="mdi mdi-open-in-new text-xs text-emerald-600"></i>
        </RouterLink>
      </div>
    </div>

    <!-- 3. KPI Summary Metrics Cards -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold text-slate-500 uppercase tracking-wide">ดีลทั้งหมด</span>
          <p class="text-2xl font-black text-slate-900 mt-1">{{ totalDealsCount }} รายการ</p>
          <span class="text-[11px] text-slate-400 font-medium">แคมเปญโปรโมชันของสินค้านี้</span>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-slate-100 flex items-center justify-center text-slate-600 shrink-0">
          <i class="mdi mdi-tag-multiple-outline text-2xl"></i>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold text-emerald-700 uppercase tracking-wide">กำลังใช้งานอยู่</span>
          <p class="text-2xl font-black text-emerald-700 mt-1">{{ activeDealsCount }} ดีล</p>
          <span class="text-[11px] text-emerald-600 font-medium">ลูกค้าสามารถรับสิทธิ์ส่วนลดได้</span>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-emerald-50 flex items-center justify-center text-emerald-600 shrink-0 border border-emerald-100">
          <i class="mdi mdi-lightning-bolt text-2xl"></i>
        </div>
      </div>

      <div class="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex items-center justify-between">
        <div>
          <span class="text-xs font-semibold text-teal-700 uppercase tracking-wide">สิทธิ์ที่ใช้ไปแล้ว</span>
          <p class="text-2xl font-black text-slate-900 mt-1">
            {{ totalUsedQuota }} <span class="text-sm font-semibold text-slate-400">/ {{ totalQuota }} สิทธิ์</span>
          </p>
          <span class="text-[11px] text-teal-600 font-medium">
            {{ totalQuota > 0 ? ((totalUsedQuota / totalQuota) * 100).toFixed(0) : 0 }}% ของโควตาทั้งหมด
          </span>
        </div>
        <div class="h-12 w-12 rounded-2xl bg-teal-50 flex items-center justify-center text-teal-600 shrink-0 border border-teal-100">
          <i class="mdi mdi-account-group-outline text-2xl"></i>
        </div>
      </div>
    </div>

    <!-- 4. Create Deal Form Card (Collapsible) -->
    <div
      v-if="showForm"
      class="rounded-2xl border-2 border-emerald-500/40 bg-gradient-to-b from-emerald-50/20 to-white p-6 shadow-md space-y-6"
    >
      <div class="flex items-center justify-between border-b border-emerald-100 pb-4">
        <div class="flex items-center gap-3">
          <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-600 text-white shadow-xs">
            <i class="mdi mdi-plus text-xl text-white"></i>
          </span>
          <div>
            <h2 class="text-lg font-bold text-slate-900">สร้างดีลโปรโมชันใหม่</h2>
            <p class="text-xs text-slate-500 font-medium">สำหรับสินค้า: {{ productName }}</p>
          </div>
        </div>

        <button
          type="button"
          class="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition"
          @click="showForm = false"
        >
          <i class="mdi mdi-close text-xl"></i>
        </button>
      </div>

      <form class="space-y-6" @submit.prevent="save">
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          <!-- Deal Type (Capsule Design) -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">ประเภทดีล *</label>
            <div class="inline-flex w-full items-center rounded-full bg-slate-100 p-1 border-2 border-slate-300 shadow-inner">
              <button
                type="button"
                class="flex-1 flex items-center justify-center gap-1.5 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                :class="
                  form.dealType === 'FlashDeal'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-900 bg-transparent'
                "
                @click="form.dealType = 'FlashDeal'"
              >
                <i class="mdi mdi-lightning-bolt text-sm"></i>
                <span>Flash Deal</span>
              </button>
              <button
                type="button"
                class="flex-1 flex items-center justify-center gap-1.5 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                :class="
                  form.dealType === 'LocalDeal'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-900 bg-transparent'
                "
                @click="form.dealType = 'LocalDeal'"
              >
                <i class="mdi mdi-storefront text-sm"></i>
                <span>โปรเด็ดชุมชน</span>
              </button>
            </div>
          </div>

          <!-- Discount Type (Capsule Design) -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">รูปแบบส่วนลด *</label>
            <div class="inline-flex w-full items-center rounded-full bg-slate-100 p-1 border-2 border-slate-300 shadow-inner">
              <button
                type="button"
                class="flex-1 flex items-center justify-center gap-1.5 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                :class="
                  form.discountType === 'FixedAmount'
                    ? 'bg-teal-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-900 bg-transparent'
                "
                @click="form.discountType = 'FixedAmount'"
              >
                <i class="mdi mdi-cash text-sm"></i>
                <span>ลดเป็นบาท (฿)</span>
              </button>
              <button
                type="button"
                class="flex-1 flex items-center justify-center gap-1.5 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                :class="
                  form.discountType === 'Percent'
                    ? 'bg-teal-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-900 bg-transparent'
                "
                @click="form.discountType = 'Percent'"
              >
                <i class="mdi mdi-percent text-sm"></i>
                <span>ลดเป็นเปอร์เซ็นต์ (%)</span>
              </button>
            </div>
          </div>

          <!-- Discount Value -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">
              มูลค่าส่วนลด ({{ form.discountType === 'Percent' ? '%' : 'บาท' }}) *
            </label>
            <input
              v-model.number="form.discountValue"
              type="number"
              min="1"
              :max="form.discountType === 'Percent' ? 99 : undefined"
              placeholder="เช่น 20 หรือ 50"
              required
              class="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-sm font-bold text-slate-900 focus:border-emerald-600 focus:ring-1 focus:ring-emerald-500 transition shadow-2xs"
            />
          </div>

          <!-- Total Quantity -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">
              จำนวนสิทธิ์ที่เปิดให้ใช้ *
            </label>
            <input
              v-model.number="form.totalQuantity"
              type="number"
              min="1"
              placeholder="เช่น 20"
              required
              class="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-sm font-bold text-slate-900 focus:border-emerald-600 focus:ring-1 focus:ring-emerald-500 transition shadow-2xs"
            />
          </div>

          <!-- Starts At -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">วัน-เวลาเริ่มต้น *</label>
            <input
              v-model="form.startsAt"
              type="datetime-local"
              required
              class="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-xs sm:text-sm font-medium text-slate-800 focus:border-emerald-600 transition shadow-2xs"
            />
          </div>

          <!-- Ends At -->
          <div class="space-y-1.5">
            <label class="block text-xs sm:text-sm font-bold text-slate-700">วัน-เวลาสิ้นสุด *</label>
            <input
              v-model="form.endsAt"
              type="datetime-local"
              required
              class="w-full rounded-xl border border-slate-300 bg-white px-3.5 py-2.5 text-xs sm:text-sm font-medium text-slate-800 focus:border-emerald-600 transition shadow-2xs"
            />
          </div>
        </div>

        <!-- Quick Presets + Real-time Calculation Summary -->
        <div class="rounded-2xl border border-emerald-100 bg-emerald-50/50 p-4 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
          <div class="flex flex-wrap items-center gap-2">
            <span class="text-xs font-black text-emerald-950 mr-1">ตั้งค่าช่วงเวลาด่วน:</span>
            <button
              type="button"
              class="rounded-full bg-white border-2 border-emerald-200 px-3.5 py-1 text-xs font-bold text-emerald-900 hover:bg-emerald-600 hover:text-white transition cursor-pointer select-none outline-none focus:outline-none"
              @click="applyPreset(24)"
            >
              24 ชม.
            </button>
            <button
              type="button"
              class="rounded-full bg-white border-2 border-emerald-200 px-3.5 py-1 text-xs font-bold text-emerald-900 hover:bg-emerald-600 hover:text-white transition cursor-pointer select-none outline-none focus:outline-none"
              @click="applyPreset(72)"
            >
              3 วัน
            </button>
            <button
              type="button"
              class="rounded-full bg-white border-2 border-emerald-200 px-3.5 py-1 text-xs font-bold text-emerald-900 hover:bg-emerald-600 hover:text-white transition cursor-pointer select-none outline-none focus:outline-none"
              @click="applyPreset(168)"
            >
              7 วัน
            </button>
            <button
              type="button"
              class="rounded-full bg-white border-2 border-emerald-200 px-3.5 py-1 text-xs font-bold text-emerald-900 hover:bg-emerald-600 hover:text-white transition cursor-pointer select-none outline-none focus:outline-none"
              @click="applyPreset(720)"
            >
              30 วัน
            </button>
          </div>

          <!-- Price Calculation Box -->
          <div v-if="currentProduct && form.discountValue > 0" class="flex items-center gap-3 text-xs bg-white rounded-full px-4 py-2 border-2 border-emerald-200">
            <span class="text-slate-500 font-semibold">ราคาปกติ ฿{{ Number(currentProduct.price).toLocaleString() }}</span>
            <span class="text-slate-300">→</span>
            <span class="font-bold text-slate-800">
              ราคาหลังลด: <strong class="text-emerald-700 text-sm font-black">฿{{ calculatedDiscountPrice.toLocaleString() }}</strong>
            </span>
            <span class="text-emerald-800 bg-emerald-100 px-2.5 py-0.5 rounded-full font-black">
              ประหยัด ฿{{ calculatedSavings.toLocaleString() }}
            </span>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="flex items-center justify-end gap-3 pt-2">
          <button
            type="button"
            class="rounded-full border-2 border-slate-200 bg-white px-5 py-2.5 text-xs sm:text-sm font-bold text-slate-700 hover:bg-slate-50 transition cursor-pointer select-none outline-none focus:outline-none"
            @click="showForm = false"
          >
            ยกเลิก
          </button>
          <button
            type="submit"
            class="inline-flex items-center gap-2 rounded-full bg-emerald-600 hover:bg-emerald-700 px-6 py-2.5 text-xs sm:text-sm font-black text-white shadow-xs transition active:scale-95 disabled:opacity-50 cursor-pointer border-2 border-emerald-600 outline-none focus:outline-none"
            :disabled="saving"
          >
            <i :class="['mdi text-base text-white', saving ? 'mdi-loading animate-spin' : 'mdi-check-circle']"></i>
            <span>{{ saving ? 'กำลังบันทึกดีล...' : 'บันทึกการสร้างดีล' }}</span>
          </button>
        </div>
      </form>
    </div>

    <!-- 5. Deal List Table Section -->
    <div class="rounded-2xl border border-slate-200/80 bg-white shadow-xs overflow-hidden">
      <div class="p-5 border-b border-slate-100 flex items-center justify-between">
        <div class="flex items-center gap-2.5">
          <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
            <i class="mdi mdi-format-list-bulleted text-lg"></i>
          </span>
          <h2 class="text-base font-bold text-slate-900">ประวัติและรายการดีลทั้งหมด</h2>
        </div>
        <span class="text-xs font-semibold text-slate-500">
          ทั้งหมด {{ deals.length }} รายการ
        </span>
      </div>

      <!-- Skeleton or Loading -->
      <div v-if="loading" class="p-12 text-center text-slate-500">
        <div class="inline-block h-8 w-8 animate-spin rounded-full border-2 border-emerald-500 border-t-transparent mb-3"></div>
        <p class="text-sm font-medium">กำลังโหลดรายการดีลสินค้า...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="deals.length === 0" class="py-16 text-center text-slate-500 space-y-4">
        <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
          <i class="mdi mdi-ticket-percent-outline text-3xl"></i>
        </div>
        <div>
          <p class="font-bold text-slate-800 text-base">ยังไม่มีโปรโมชันสำหรับสินค้านี้</p>
          <p class="text-xs text-slate-500 mt-1 max-w-md mx-auto">
            สร้างดีล Flash Deal หรือโปรเด็ดชุมชนเพื่อดึงดูดลูกค้าและเพิ่มยอดขายให้กับสินค้านี้ได้ทันที
          </p>
        </div>
        <button
          type="button"
          class="inline-flex items-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-5 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition"
          @click="openCreateForm"
        >
          <i class="mdi mdi-plus-circle text-base"></i>
          <span>สร้างดีลแรกสำหรับสินค้านี้</span>
        </button>
      </div>

      <!-- Deals Table -->
      <div v-else class="overflow-x-auto">
        <table class="w-full text-left border-collapse text-sm">
          <thead>
            <tr class="border-b border-slate-100 bg-slate-50/70 text-xs font-bold text-slate-500 uppercase tracking-wider">
              <th class="py-3.5 px-5">ประเภทดีล</th>
              <th class="py-3.5 px-5">ส่วนลดและราคา</th>
              <th class="py-3.5 px-5">สิทธิ์คงเหลือ</th>
              <th class="py-3.5 px-5">ระยะเวลาแคมเปญ</th>
              <th class="py-3.5 px-5">สถานะ</th>
              <th class="py-3.5 px-5 text-right">จัดการ</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100 font-medium text-slate-700">
            <tr
              v-for="deal in deals"
              :key="deal.productDealId"
              class="hover:bg-slate-50/60 transition"
            >
              <!-- Deal Type -->
              <td class="py-4 px-5">
                <div class="flex items-center gap-2.5">
                  <span
                    class="flex h-8 w-8 items-center justify-center rounded-xl shrink-0"
                    :class="
                      deal.dealType === 'FlashDeal'
                        ? 'bg-amber-50 text-amber-600'
                        : 'bg-emerald-50 text-emerald-600'
                    "
                  >
                    <i
                      :class="[
                        'mdi text-lg',
                        deal.dealType === 'FlashDeal' ? 'mdi-lightning-bolt' : 'mdi-storefront',
                      ]"
                    ></i>
                  </span>
                  <div>
                    <span class="font-bold text-slate-900 block text-xs sm:text-sm">
                      {{ label(deal) }}
                    </span>
                    <span class="text-[11px] text-slate-400">
                      {{ deal.dealType }}
                    </span>
                  </div>
                </div>
              </td>

              <!-- Discount & Price -->
              <td class="py-4 px-5">
                <div class="space-y-0.5">
                  <span class="inline-flex items-center gap-1 rounded-md bg-rose-50 text-rose-700 border border-rose-100 px-2 py-0.5 text-xs font-black">
                    <i class="mdi mdi-arrow-down text-xs"></i>
                    {{ deal.discountType === 'Percent' ? `${deal.discountValue}%` : `฿${deal.discountValue}` }}
                  </span>
                  <div v-if="getDealDiscountedPrice(deal) !== null" class="text-xs text-slate-500 mt-0.5">
                    เหลือเพียง
                    <strong class="font-bold text-emerald-700">
                      ฿{{ getDealDiscountedPrice(deal)?.toLocaleString() }}
                    </strong>
                  </div>
                </div>
              </td>

              <!-- Quota / Remaining -->
              <td class="py-4 px-5">
                <div class="space-y-1.5 max-w-[140px]">
                  <div class="flex items-center justify-between text-xs">
                    <span class="font-bold text-slate-800">{{ deal.remainingQuantity }}</span>
                    <span class="text-slate-400">/ {{ deal.totalQuantity }} สิทธิ์</span>
                  </div>
                  <div class="h-1.5 w-full bg-slate-100 rounded-full overflow-hidden">
                    <div
                      class="h-full rounded-full transition-all duration-300"
                      :class="
                        deal.remainingQuantity === 0
                          ? 'bg-rose-500'
                          : deal.remainingQuantity <= 5
                            ? 'bg-amber-500'
                            : 'bg-emerald-500'
                      "
                      :style="{
                        width: `${Math.min(100, Math.round((deal.remainingQuantity / deal.totalQuantity) * 100))}%`,
                      }"
                    ></div>
                  </div>
                </div>
              </td>

              <!-- Period (Campaign Duration with Live Status & Expired Stop Deal indication) -->
              <td class="py-4 px-5 text-xs">
                <div class="space-y-1 text-slate-700">
                  <div class="flex items-center gap-1.5 text-slate-600 font-semibold">
                    <i class="mdi mdi-clock-start text-emerald-600 text-xs"></i>
                    <span>เริ่ม: {{ formatThaiDate(deal.startsAt) }}</span>
                  </div>
                  <div class="flex items-center gap-1.5 text-slate-600 font-semibold">
                    <i class="mdi mdi-clock-end text-rose-500 text-xs"></i>
                    <span>สิ้นสุด: {{ formatThaiDate(deal.endsAt) }}</span>
                  </div>

                  <!-- Campaign Duration Indicator Pill -->
                  <div class="pt-0.5">
                    <span
                      v-if="deal.status === 'Stopped'"
                      class="inline-flex items-center gap-1 rounded-full bg-slate-100 border border-slate-200 px-2.5 py-0.5 text-[10px] font-bold text-slate-600"
                    >
                      <i class="mdi mdi-stop-circle text-xs text-slate-400"></i>
                      <span>หยุดดีลแล้ว</span>
                    </span>
                    <span
                      v-else-if="isDealExpired(deal)"
                      class="inline-flex items-center gap-1 rounded-full bg-rose-50 border border-rose-200 px-2.5 py-0.5 text-[10px] font-black text-rose-700"
                    >
                      <i class="mdi mdi-clock-alert text-xs text-rose-600"></i>
                      <span>หมดระยะเวลาแคมเปญ (หยุดดีล)</span>
                    </span>
                    <span
                      v-else-if="!isDealStarted(deal)"
                      class="inline-flex items-center gap-1 rounded-full bg-blue-50 border border-blue-200 px-2.5 py-0.5 text-[10px] font-bold text-blue-700"
                    >
                      <i class="mdi mdi-clock-outline text-xs text-blue-500"></i>
                      <span>รอเริ่มแคมเปญ</span>
                    </span>
                    <span
                      v-else
                      class="inline-flex items-center gap-1 rounded-full bg-emerald-50 border border-emerald-200 px-2.5 py-0.5 text-[10px] font-bold text-emerald-800"
                    >
                      <i class="mdi mdi-timer-sand text-xs text-emerald-600"></i>
                      <span>{{ getRemainingTimeText(deal.endsAt) }}</span>
                    </span>
                  </div>
                </div>
              </td>

              <!-- Status -->
              <td class="py-4 px-5">
                <span
                  v-if="deal.status === 'Stopped'"
                  class="inline-flex items-center gap-1.5 rounded-full bg-slate-100 border border-slate-300 px-3 py-1 text-xs font-bold text-slate-700"
                >
                  <span class="h-2 w-2 rounded-full bg-slate-400"></span>
                  <span>หยุดดีลแล้ว</span>
                </span>
                <span
                  v-else-if="isDealExpired(deal)"
                  class="inline-flex items-center gap-1.5 rounded-full bg-rose-50 border border-rose-300 px-3 py-1 text-xs font-bold text-rose-800"
                >
                  <span class="h-2 w-2 rounded-full bg-rose-500"></span>
                  <span>หมดระยะเวลา (หยุดดีล)</span>
                </span>
                <span
                  v-else-if="!isDealStarted(deal)"
                  class="inline-flex items-center gap-1.5 rounded-full bg-blue-50 border border-blue-300 px-3 py-1 text-xs font-bold text-blue-800"
                >
                  <span class="h-2 w-2 rounded-full bg-blue-500"></span>
                  <span>รอเริ่มแคมเปญ</span>
                </span>
                <span
                  v-else
                  class="inline-flex items-center gap-1.5 rounded-full bg-emerald-50 border border-emerald-300 px-3 py-1 text-xs font-bold text-emerald-800 shadow-2xs"
                >
                  <span class="h-2 w-2 rounded-full bg-emerald-600 animate-pulse"></span>
                  <span>กำลังใช้งาน</span>
                </span>
              </td>

              <!-- Actions (Stop Deal Capsule Button) -->
              <td class="py-4 px-5 text-right">
                <button
                  v-if="deal.status !== 'Stopped'"
                  type="button"
                  class="inline-flex items-center gap-1.5 rounded-full border-2 border-rose-500 bg-rose-50 hover:bg-rose-600 hover:text-white text-rose-800 px-4 py-1.5 text-xs font-black transition cursor-pointer select-none shadow-2xs active:scale-95 outline-none focus:outline-none"
                  :title="isDealExpired(deal) ? 'หยุดและปิดดีลนี้' : 'หยุดการใช้งานดีลนี้'"
                  @click="handleStopDeal(deal.productDealId)"
                >
                  <i class="mdi mdi-stop-circle-outline text-sm"></i>
                  <span>หยุดดีล</span>
                </button>
                <span v-else class="inline-flex items-center gap-1 text-xs text-slate-400 font-bold px-2 py-1">
                  <i class="mdi mdi-check text-xs"></i>
                  <span>หยุดดีลแล้ว</span>
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
button,
a,
input,
select,
[role="button"] {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active,
a:focus,
a:focus-visible,
a:active,
input:focus,
input:focus-visible,
input:active,
select:focus,
select:focus-visible,
select:active {
  outline: none !important;
  box-shadow: none !important;
}
</style>
