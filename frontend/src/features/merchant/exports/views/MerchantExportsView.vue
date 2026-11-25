<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useSwal } from '@/plugins/sweetalert'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import AppDateRangeFilter, { type DatePreset } from '@/shared/components/AppDateRangeFilter.vue'
import { exportMerchantReport } from '../api/merchantExportApi'
import type { ExportFormat, ExportOption, MerchantExportType } from '../interface/type'

const reportOptions: (ExportOption & { badge: string; detailedDesc: string })[] = [
  {
    value: 'orders',
    title: 'รายงานคำสั่งซื้อ',
    description: 'ข้อมูลคำสั่งซื้อทั้งหมด ยอดเงินรวม ค่าจัดส่ง ข้อมูลผู้ซื้อ และสถานะการจัดส่ง',
    detailedDesc: 'เหมาะสำหรับตรวจสอบยอดคำสั่งซื้อและนำไปใช้วิเคราะห์การขาย',
    icon: 'mdi-receipt-text-outline',
    badge: 'ยอดนิยม',
  },
  {
    value: 'products',
    title: 'ยอดขายรายสินค้า',
    description: 'สรุปยอดขายแยกตามรายชื่อสินค้า จำนวนชิ้นที่ขายได้ เฉพาะออเดอร์ที่ชำระเงินแล้ว',
    detailedDesc: 'เหมาะสำหรับดูสินค้าขายดี สินค้าทำเงิน และจัดการสต็อก',
    icon: 'mdi-package-variant-closed',
    badge: 'เชิงลึก',
  },
  {
    value: 'payouts',
    title: 'ประวัติการรับเงินโอน',
    description: 'รายการเงินยอดขายที่แพลตฟอร์มสั่งโอนคืนให้ร้านค้า พร้อมเลขอ้างอิงและวันที่โอน',
    detailedDesc: 'เหมาะสำหรับใช้กระทบยอดบัญชีรายรับของร้านค้ากับสลิปโอนเงิน',
    icon: 'mdi-cash-multiple',
    badge: 'การเงิน',
  },
]

const orderStatusOptions = [
  { label: 'ทุกสถานะ', value: '' },
  { label: 'รอดำเนินการ', value: 'Pending' },
  { label: 'ยืนยันออเดอร์', value: 'Confirmed' },
  { label: 'กำลังเตรียมส่ง', value: 'Processing' },
  { label: 'กำลังจัดส่ง', value: 'Shipped' },
  { label: 'สำเร็จแล้ว', value: 'Completed' },
  { label: 'ยกเลิก', value: 'Cancelled' },
]

const selectedType = ref<MerchantExportType>('orders')
const format = ref<ExportFormat>('csv')
const datePreset = ref<DatePreset>('30days')
const startDate = ref('')
const endDate = ref('')
const orderStatus = ref('')
const exporting = ref(false)
const swal = useSwal()

const selectedReport = computed(
  () => reportOptions.find((option) => option.value === selectedType.value) ?? reportOptions[0]!,
)
const isOrdersReport = computed(() => selectedType.value === 'orders')

function toDateInputValue(date: Date) {
  return date.toISOString().slice(0, 10)
}

function applyDatePreset(preset: DatePreset) {
  if (preset === 'all' || preset === 'custom') return
  const end = new Date()
  const start = new Date(end)
  if (preset === '7days') start.setDate(start.getDate() - 6)
  if (preset === '30days') start.setDate(start.getDate() - 29)
  startDate.value = toDateInputValue(start)
  endDate.value = toDateInputValue(end)
}

function clearDateFilter() {
  startDate.value = ''
  endDate.value = ''
}
watch(datePreset, applyDatePreset, { immediate: true })

// Helper display text for date range
const dateRangeLabel = computed(() => {
  if (datePreset.value === '7days') return '7 วันล่าสุด'
  if (datePreset.value === '30days') return '30 วันล่าสุด'
  if (datePreset.value === 'all') return 'ข้อมูลทั้งหมดตั้งแต่เริ่มเปิดร้าน'
  if (startDate.value && endDate.value) return `${startDate.value} ถึง ${endDate.value}`
  if (startDate.value) return `ตั้งแต่ ${startDate.value}`
  if (endDate.value) return `ถึง ${endDate.value}`
  return 'ไม่ได้ระบุช่วงวันที่'
})

async function downloadReport() {
  if (datePreset.value === 'custom' && (!startDate.value || !endDate.value)) {
    await swal.error('กรุณาระบุช่วงวันที่', 'เลือกวันที่เริ่มต้นและวันที่สิ้นสุดก่อนส่งออกรายงาน')
    return
  }
  if (startDate.value && endDate.value && startDate.value > endDate.value) {
    await swal.error('ช่วงวันที่ไม่ถูกต้อง', 'วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด')
    return
  }
  exporting.value = true
  try {
    await exportMerchantReport(selectedType.value, {
      from: startDate.value || undefined,
      to: endDate.value || undefined,
      orderStatus: isOrdersReport.value ? orderStatus.value || undefined : undefined,
      format: format.value,
    })
    await swal.success(
      'ส่งออกรายงานสำเร็จ',
      `กำลังดาวน์โหลดไฟล์ ${format.value.toUpperCase()} ของคุณลงในเครื่อง`,
    )
  } catch (error) {
    await swal.error('ส่งออกรายงานไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    exporting.value = false
  }
}
</script>

<template>
  <div class="mx-auto w-full max-w-[1920px] space-y-6 pb-16 text-slate-900 font-sans">
    <!-- Header Banner -->
    <div class="flex flex-wrap items-center justify-between gap-4 border-b-2 border-slate-200 pb-6">
      <div>
        <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-2">
          <i class="mdi mdi-file-download-outline text-emerald-600"></i>
          <span>ศูนย์ส่งออกรายงานและข้อมูลร้านค้า</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-black text-slate-950 tracking-tight">
          ส่งออกรายงานร้านค้า (Merchant Exports)
        </h1>
        <p class="mt-1 text-xs sm:text-sm text-slate-600 font-bold">
          ดาวน์โหลดข้อมูลคำสั่งซื้อ ยอดขายรายสินค้า และประวัติการรับเงินในรูปแบบไฟล์ CSV หรือ PDF
        </p>
      </div>

      <!-- Live File Indicator Capsule -->
      <div class="inline-flex items-center gap-2 rounded-full bg-slate-100 px-4 py-2 border-2 border-slate-300">
        <span class="h-2.5 w-2.5 rounded-full bg-emerald-600 animate-pulse"></span>
        <span class="text-xs font-bold text-slate-700">รูปแบบไฟล์ที่เลือก:</span>
        <span class="rounded-full bg-emerald-600 text-white px-2.5 py-0.5 text-xs font-black uppercase">
          {{ format }}
        </span>
      </div>
    </div>

    <!-- Main Grid: Configuration Form (Left 8 Cols) + Live Summary Preview (Right 4 Cols) -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
      <!-- Left Column: Step-by-Step Configuration (8 Cols) -->
      <div class="lg:col-span-8 space-y-6">
        <!-- Step 1: Select Report Type -->
        <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-4">
          <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3.5">
            <div class="flex items-center gap-2.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-800 font-bold">
                <i class="mdi mdi-file-chart-outline text-xl text-emerald-800"></i>
              </span>
              <div>
                <h2 class="text-base font-black text-slate-950">1. เลือกประเภทรายงานที่ต้องการ</h2>
                <p class="text-xs font-bold text-slate-600">
                  ระบบจะประมวลผลข้อมูลที่เกี่ยวข้องกับร้านค้าของคุณ
                </p>
              </div>
            </div>
            <span class="text-xs font-bold text-slate-500">ขั้นตอนที่ 1 จาก 3</span>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-3 gap-4 pt-1">
            <button
              v-for="option in reportOptions"
              :key="option.value"
              type="button"
              class="relative rounded-2xl border-2 p-4 text-left transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none flex flex-col justify-between space-y-3"
              :class="
                selectedType === option.value
                  ? 'border-emerald-600 bg-emerald-50/50 shadow-sm ring-1 ring-emerald-500/30'
                  : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50/50'
              "
              @click="selectedType = option.value"
            >
              <div>
                <div class="flex items-start justify-between gap-2">
                  <div
                    class="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl transition"
                    :class="
                      selectedType === option.value
                        ? 'bg-emerald-600 text-white shadow-xs'
                        : 'bg-slate-100 text-slate-700'
                    "
                  >
                    <i class="mdi text-2xl" :class="option.icon"></i>
                  </div>
                  <span
                    class="rounded-full px-2.5 py-0.5 text-[11px] font-black border"
                    :class="
                      selectedType === option.value
                        ? 'bg-emerald-600 text-white border-emerald-600'
                        : 'bg-slate-100 text-slate-600 border-slate-200'
                    "
                  >
                    {{ option.badge }}
                  </span>
                </div>

                <h3 class="text-sm font-black text-slate-950 mt-3">{{ option.title }}</h3>
                <p class="mt-1 text-xs font-semibold leading-relaxed text-slate-600">
                  {{ option.description }}
                </p>
              </div>

              <!-- Selected Active Indicator -->
              <div class="pt-2 border-t border-slate-100 flex items-center justify-between">
                <span class="text-[11px] font-bold text-slate-500">{{ option.detailedDesc }}</span>
                <span
                  v-if="selectedType === option.value"
                  class="flex h-5 w-5 items-center justify-center rounded-full bg-emerald-600 text-white text-xs shrink-0"
                >
                  <i class="mdi mdi-check"></i>
                </span>
              </div>
            </button>
          </div>
        </section>

        <!-- Step 2: Date Range & Order Status Filters -->
        <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-5">
          <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3.5">
            <div class="flex items-center gap-2.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-2xl bg-teal-100 text-teal-800 font-bold">
                <i class="mdi mdi-filter-variant text-xl text-teal-800"></i>
              </span>
              <div>
                <h2 class="text-base font-black text-slate-950">2. กำหนดช่วงเวลาและเงื่อนไขตัวกรอง</h2>
                <p class="text-xs font-bold text-slate-600">
                  เลือกช่วงวันที่ต้องการ หรือระบุสถานะคำสั่งซื้อเจาะจง
                </p>
              </div>
            </div>
            <span class="text-xs font-bold text-slate-500">ขั้นตอนที่ 2 จาก 3</span>
          </div>

          <!-- Date Range Filter Component -->
          <div class="space-y-2">
            <label class="block text-xs font-bold text-slate-700">ช่วงเวลาของข้อมูล</label>
            <div class="rounded-xl border border-slate-200 bg-slate-50/60 p-4">
              <AppDateRangeFilter
                v-model:preset="datePreset"
                v-model:start-date="startDate"
                v-model:end-date="endDate"
                @clear="clearDateFilter"
              />
            </div>
          </div>

          <!-- Additional Orders Filter (Capsule Pills) -->
          <div v-if="isOrdersReport" class="border-t border-slate-100 pt-5 space-y-2.5">
            <div class="flex items-center justify-between">
              <label class="block text-xs font-bold text-slate-700">
                กรองตามสถานะออเดอร์ (เลือกได้ 1 สถานะ)
              </label>
              <span v-if="orderStatus" class="text-xs font-bold text-emerald-700 cursor-pointer hover:underline" @click="orderStatus = ''">
                ล้างตัวกรองสถานะ
              </span>
            </div>

            <!-- Capsule Segmented Filter Pills -->
            <div class="flex flex-wrap gap-2 pt-1">
              <button
                v-for="st in orderStatusOptions"
                :key="st.value"
                type="button"
                class="rounded-full px-3.5 py-1.5 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none border-2"
                :class="
                  orderStatus === st.value
                    ? 'border-emerald-600 bg-emerald-600 text-white shadow-xs'
                    : 'border-slate-200 bg-slate-50 text-slate-700 hover:border-slate-300 hover:bg-slate-100'
                "
                @click="orderStatus = st.value"
              >
                {{ st.label }}
              </button>
            </div>
          </div>
        </section>

        <!-- Step 3: Format Selector & Download Action -->
        <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-5">
          <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3.5">
            <div class="flex items-center gap-2.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-2xl bg-indigo-100 text-indigo-800 font-bold">
                <i class="mdi mdi-download-box-outline text-xl text-indigo-800"></i>
              </span>
              <div>
                <h2 class="text-base font-black text-slate-950">3. เลือกนามสกุลไฟล์และดาวน์โหลด</h2>
                <p class="text-xs font-bold text-slate-600">
                  เลือกรูปแบบไฟล์ที่ต้องการเพื่อนำไปใช้งานต่อ
                </p>
              </div>
            </div>
            <span class="text-xs font-bold text-slate-500">ขั้นตอนที่ 3 จาก 3</span>
          </div>

          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pt-1">
            <div class="space-y-1">
              <label class="block text-xs font-bold text-slate-700">รูปแบบไฟล์ส่งออก *</label>
              <p class="text-xs text-slate-500 font-semibold">
                {{ format === 'csv' ? 'ไฟล์ CSV สามารถเปิดได้ด้วยโปรแกรม Microsoft Excel, Numbers หรือ Google Sheets' : 'ไฟล์ PDF เป็นเอกสารพิมพ์ พร้อมตารางสรุปที่สวยงามและพิมพ์ออกกระดาษได้ทันที' }}
              </p>
            </div>

            <!-- Format Toggle (Capsule Segmented Pill) -->
            <div class="inline-flex items-center rounded-full bg-slate-100 p-1 border-2 border-slate-300 shadow-inner shrink-0">
              <button
                type="button"
                class="flex items-center gap-2 rounded-full px-5 py-2 text-xs font-black transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none"
                :class="
                  format === 'csv'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-950 bg-transparent'
                "
                @click="format = 'csv'"
              >
                <i class="mdi mdi-file-delimited-outline text-base"></i>
                <span>CSV (Excel)</span>
              </button>
              <button
                type="button"
                class="flex items-center gap-2 rounded-full px-5 py-2 text-xs font-black transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none"
                :class="
                  format === 'pdf'
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'text-slate-700 hover:text-slate-950 bg-transparent'
                "
                @click="format = 'pdf'"
              >
                <i class="mdi mdi-file-pdf-box text-base"></i>
                <span>PDF Document</span>
              </button>
            </div>
          </div>
        </section>
      </div>

      <!-- Right Column: Live Summary & Action Card (4 Cols Sticky) -->
      <aside class="lg:col-span-4 space-y-5 lg:sticky lg:top-6">
        <!-- Live Export Summary Card -->
        <div class="rounded-2xl border-2 border-emerald-200 bg-white p-5 shadow-xs space-y-4">
          <div class="flex items-center justify-between border-b-2 border-emerald-100 pb-3">
            <span class="text-xs font-black uppercase tracking-wider text-emerald-900 flex items-center gap-1.5">
              <i class="mdi mdi-file-check-outline text-emerald-700 text-base"></i>
              สรุปข้อมูลที่จะส่งออก
            </span>
            <span class="rounded-full bg-emerald-100 text-emerald-800 px-2 py-0.5 text-[11px] font-black">
              Ready
            </span>
          </div>

          <div class="space-y-3 text-xs">
            <div class="flex items-start justify-between py-1 border-b border-slate-100">
              <span class="font-bold text-slate-500">ประเภทรายงาน:</span>
              <span class="font-black text-slate-950 text-right">{{ selectedReport.title }}</span>
            </div>

            <div class="flex items-start justify-between py-1 border-b border-slate-100">
              <span class="font-bold text-slate-500">รูปแบบไฟล์:</span>
              <span class="inline-flex items-center gap-1 font-black text-emerald-700 uppercase">
                <i :class="format === 'csv' ? 'mdi mdi-file-delimited-outline' : 'mdi mdi-file-pdf-box'"></i>
                {{ format.toUpperCase() }}
              </span>
            </div>

            <div class="flex items-start justify-between py-1 border-b border-slate-100">
              <span class="font-bold text-slate-500">ช่วงเวลาข้อมูล:</span>
              <span class="font-bold text-slate-900 text-right max-w-[170px]">{{ dateRangeLabel }}</span>
            </div>

            <div v-if="isOrdersReport" class="flex items-start justify-between py-1 border-b border-slate-100">
              <span class="font-bold text-slate-500">ตัวกรองสถานะ:</span>
              <span class="font-bold text-slate-900">
                {{ orderStatusOptions.find(o => o.value === orderStatus)?.label || 'ทุกสถานะ' }}
              </span>
            </div>
          </div>

          <!-- Download Action Button (Capsule Style) -->
          <div class="pt-2">
            <button
              type="button"
              class="w-full inline-flex items-center justify-center gap-2 rounded-full bg-emerald-700 hover:bg-emerald-800 px-6 py-3 font-black text-sm text-white shadow-md transition active:scale-95 disabled:cursor-not-allowed disabled:opacity-60 cursor-pointer select-none outline-none focus:outline-none"
              :disabled="exporting"
              @click="downloadReport"
            >
              <i
                class="mdi text-lg text-white"
                :class="exporting ? 'mdi-loading animate-spin' : 'mdi-download'"
              />
              <span>{{ exporting ? 'กำลังสร้างไฟล์รายงาน...' : `ดาวน์โหลดไฟล์ ${format.toUpperCase()}` }}</span>
            </button>
          </div>

          <p class="text-[11px] text-center text-slate-500 font-semibold leading-relaxed pt-1">
            ไฟล์จะถูกบันทึกไปยังโฟลเดอร์ดาวน์โหลดบนเครื่องของคุณโดยอัตโนมัติ
          </p>
        </div>

        <!-- Help & Information Tips Card -->
        <div class="rounded-2xl border-2 border-slate-200 bg-slate-50/70 p-5 space-y-2.5">
          <div class="flex items-center gap-2 text-xs font-black text-slate-900">
            <i class="mdi mdi-lightbulb-on-outline text-amber-600 text-base"></i>
            <span>คำแนะนำการใช้งานรายงาน</span>
          </div>
          <ul class="text-xs text-slate-600 space-y-1.5 font-medium leading-relaxed">
            <li>• หากต้องการเปิดใน Excel และแสดงภาษาไทยถูกต้อง ให้เลือกไฟล์ CSV</li>
            <li>• รายงานประวัติการรับเงินจะแสดงรอบที่แอดมินโอนเข้าบัญชีสำเร็จแล้ว</li>
            <li>• สามารถเลือกช่วงวันที่ย้อนหลังได้ไม่จำกัด</li>
          </ul>
        </div>
      </aside>
    </div>
  </div>
</template>
