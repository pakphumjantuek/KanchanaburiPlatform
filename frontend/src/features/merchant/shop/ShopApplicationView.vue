<script setup lang="ts">
import axios from "axios"
import { computed, onMounted, ref, watch } from "vue"
import { useRouter, RouterLink } from "vue-router"
import {
  createShop,
  getDistricts,
  getMyShop,
  getShopCategories,
  getSubDistricts,
  updateShop,
  type District,
  type Shop,
  type ShopCategory,
  type ShopFormData,
  type SubDistrict,
} from "@/features/merchant/api/shopApi"
import AppSelect from "@/components/common/input/AppSelect.vue"
import AppTextField from "@/components/common/input/AppTextField.vue"
import AppTextarea from "@/components/common/input/AppTextarea.vue"
import LocationPickerMap from "@/components/common/map/LocationPickerMap.vue"
import { getApiErrorMessage } from "@/features/auth/api/getApiErrorMessage"
import { useSwal } from "@/plugins/sweetalert"

const router = useRouter()
const swal = useSwal()

const shop = ref<Shop | null>(null)
const categories = ref<ShopCategory[]>([])
const districts = ref<District[]>([])
const subDistricts = ref<SubDistrict[]>([])
const loading = ref(true)
const saving = ref(false)
const isEditingPending = ref(false)

const form = ref<ShopFormData>({
  shopName: "",
  shopCategoryId: null,
  districtId: null,
  subDistrictId: null,
  description: "",
  phone: "",
  email: "",
  address: "",
  openingTime: "",
  closingTime: "",
  latitude: null,
  longitude: null,
  bankName: "",
  bankAccountName: "",
  bankAccountNumber: "",
  promptPay: "",
})

const popularBanks = [
  "ธนาคารกสิกรไทย",
  "ธนาคารไทยพาณิชย์",
  "ธนาคารกรุงไทย",
  "ธนาคารกรุงเทพ",
  "ธนาคารกรุงศรีอยุธยา",
  "ธนาคารทหารไทยธนชาต",
  "ธนาคารออมสิน",
  "ธ.ก.ส.",
]

const popularHours = [
  { label: "08:00 - 17:00 (เวลาทำการ)", open: "08:00", close: "17:00" },
  { label: "09:00 - 18:00 (ร้านค้าทั่วไป)", open: "09:00", close: "18:00" },
  { label: "10:00 - 20:00 (ร้านอาหาร/คาเฟ่)", open: "10:00", close: "20:00" },
  { label: "06:00 - 14:00 (ตลาดเช้า)", open: "06:00", close: "14:00" },
]

// Progress Indicators
const isSection1Complete = computed(() => Boolean(form.value.shopName?.trim() && form.value.shopCategoryId))
const isSection2Complete = computed(() => Boolean(form.value.districtId && form.value.subDistrictId))
const isSection3Complete = computed(() => Boolean(form.value.bankName?.trim() && form.value.bankAccountNumber?.trim() && form.value.bankAccountName?.trim()))

const completedSectionsCount = computed(() => {
  let count = 0
  if (isSection1Complete.value) count++
  if (isSection2Complete.value) count++
  if (isSection3Complete.value) count++
  return count
})

const isFormValid = computed(() => isSection1Complete.value && isSection2Complete.value)

function applyShop(data: Shop) {
  shop.value = data
  form.value = {
    shopName: data.shopName,
    shopCategoryId: data.shopCategoryId,
    districtId: data.districtId,
    subDistrictId: data.subDistrictId,
    description: data.description ?? "",
    phone: data.phone ?? "",
    email: data.email ?? "",
    address: data.address ?? "",
    openingTime: data.openingTime ?? "",
    closingTime: data.closingTime ?? "",
    latitude: data.latitude ?? null,
    longitude: data.longitude ?? null,
    bankName: data.bankName ?? "",
    bankAccountName: data.bankAccountName ?? "",
    bankAccountNumber: data.bankAccountNumber ?? "",
    promptPay: data.promptPay ?? "",
  }
}

async function loadSubDistricts(districtId: string | null, keepValue = false) {
  if (!districtId) {
    subDistricts.value = []
    form.value.subDistrictId = null
    return
  }
  subDistricts.value = await getSubDistricts(districtId)
  if (!keepValue) form.value.subDistrictId = null
}

function onAddressDetected(location: {
  districtName?: string
  subDistrictName?: string
  formattedAddress?: string
  fullAddress?: string
}) {
  if (location.districtName) {
    const foundDistrict = districts.value.find((d) => d.districtName.trim() === location.districtName?.trim())
    if (foundDistrict) {
      form.value.districtId = foundDistrict.districtId
      loadSubDistricts(foundDistrict.districtId, true).then(() => {
        if (location.subDistrictName) {
          const foundSubDistrict = subDistricts.value.find((sd) => sd.subDistrictName.trim() === location.subDistrictName?.trim())
          if (foundSubDistrict) {
            form.value.subDistrictId = foundSubDistrict.subDistrictId
          }
        }
      })
    }
  }
  const addr = location.fullAddress || location.formattedAddress
  if (addr && !form.value.address) {
    form.value.address = addr
  }
}

function applyOperatingHours(open: string, close: string) {
  form.value.openingTime = open
  form.value.closingTime = close
}

function selectBank(bankName: string) {
  form.value.bankName = bankName
}

function getSelectedCategoryName(): string {
  if (!form.value.shopCategoryId) return ""
  return categories.value.find((c) => c.shopCategoryId === form.value.shopCategoryId)?.categoryName || ""
}

function getSelectedDistrictName(): string {
  if (!form.value.districtId) return ""
  return districts.value.find((d) => d.districtId === form.value.districtId)?.districtName || ""
}

function getSelectedSubDistrictName(): string {
  if (!form.value.subDistrictId) return ""
  return subDistricts.value.find((sd) => sd.subDistrictId === form.value.subDistrictId)?.subDistrictName || ""
}

function scrollToSection(sectionId: string) {
  const el = document.getElementById(sectionId)
  if (el) {
    el.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }
}

watch(
  () => form.value.districtId,
  (value, oldValue) => {
    if (value && value !== oldValue) loadSubDistricts(value)
  },
)

async function checkStatus() {
  try {
    const myShop = await getMyShop()
    if (myShop.status === "Active") {
      await router.replace("/my-shop")
      return
    }
    applyShop(myShop)
    await loadSubDistricts(myShop.districtId, true)
  } catch (error) {
    if (!axios.isAxiosError(error) || error.response?.status !== 404) {
      console.error(error)
    }
  }
}

async function submit() {
  if (!form.value.shopName || !form.value.shopCategoryId || !form.value.districtId || !form.value.subDistrictId) {
    await swal.warning("กรอกข้อมูลไม่ครบถ้วน", "กรุณาระบุชื่อร้านค้า หมวดหมู่ อำเภอ และตำบล ให้ครบถ้วนก่อนส่งใบสมัคร")
    return
  }

  saving.value = true
  try {
    if (shop.value) {
      await updateShop(shop.value.shopId, form.value)
    } else {
      const created = await createShop(form.value)
      applyShop(created)
    }

    await swal.success("ส่งใบสมัครเรียบร้อยแล้ว", "เจ้าหน้าที่จะตรวจสอบข้อมูลและอนุมัติร้านค้าภายใน 1-2 วันทำการ")
    if (shop.value) shop.value.status = "PendingApproval"
    isEditingPending.value = false
  } catch (error) {
    await swal.error("ส่งใบสมัครไม่สำเร็จ", getApiErrorMessage(error, "กรุณาลองใหม่อีกครั้ง"))
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    ;[categories.value, districts.value] = await Promise.all([getShopCategories(), getDistricts()])
    await checkStatus()
  } catch (error) {
    await swal.error("โหลดข้อมูลเริ่มต้นไม่สำเร็จ", getApiErrorMessage(error, "กรุณาลองใหม่อีกครั้ง"))
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="shop-apply min-h-screen bg-[#F7F0E6] py-8 lg:py-12">
    <!-- WIDE CONTAINER: MAX-W-7XL -->
    <div class="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-10 space-y-8">
      <!-- TOP NAVIGATION BAR -->
      <div class="flex items-center justify-between border-b border-[#E8D9C9] pb-4">
        <RouterLink
          to="/"
          class="inline-flex items-center gap-2 text-xs sm:text-sm font-semibold text-[#786B62] hover:text-[#D96C2C] transition cursor-pointer"
        >
          <i class="mdi mdi-arrow-left text-lg text-[#A89B90]" />
          <span>กลับสู่หน้าหลัก</span>
        </RouterLink>

        <div class="flex items-center gap-3">
          <span class="inline-flex items-center gap-1.5 rounded-full bg-[#FFF1E6] border border-[#F6D9C2] px-3 py-1 text-xs font-bold text-[#BF5720]">
            <i class="mdi mdi-store-cog text-sm" />
            <span>สมัครเปิดร้านค้า</span>
          </span>
        </div>
      </div>

      <!-- SKELETON LOADING -->
      <div v-if="loading" class="h-96 animate-pulse rounded-3xl bg-[#E8D9C9]" />

      <template v-else>
        <!-- SPACIOUS HERO BANNER -->
        <section class="relative overflow-hidden rounded-3xl bg-gradient-to-br from-[#332820] via-[#4A3226] to-[#BF5720] p-8 sm:p-12 text-white shadow-xl">
          <!-- Background decorative elements -->
          <div class="absolute -right-20 -top-20 h-80 w-80 rounded-full bg-amber-300/20 blur-3xl pointer-events-none" />
          <div class="absolute -bottom-20 -left-20 h-80 w-80 rounded-full bg-[#D96C2C]/30 blur-3xl pointer-events-none" />

          <div class="relative z-10 flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8">
            <div class="max-w-3xl space-y-3">
              <div class="inline-flex items-center gap-2 rounded-xl bg-white/10 px-3.5 py-1.5 text-xs font-bold text-amber-300 backdrop-blur-md border border-white/15">
                <i class="mdi mdi-store-plus text-base" />
                <span>ใช้เวลาประมาณ 5 นาที</span>
              </div>

              <h1 class="text-2xl sm:text-4xl lg:text-5xl font-extrabold tracking-tight leading-tight">
                เปิดร้านของคุณบนเมืองกาญจน์
              </h1>

              <p class="text-sm sm:text-base text-amber-50/90 leading-relaxed max-w-2xl">
                กรอกข้อมูลร้านเพียง 3 ขั้นตอน ไม่ต้องมีความรู้คอมพิวเตอร์ เจ้าหน้าที่จะตรวจสอบและแจ้งผลให้ภายใน 1-2 วันทำการ
              </p>
            </div>

            <!-- KEY BENEFIT METRICS -->
            <div class="grid grid-cols-1 sm:grid-cols-3 lg:grid-cols-1 gap-3 shrink-0 lg:w-72">
              <div class="flex items-center gap-3 rounded-2xl bg-white/5 border border-white/10 p-3.5 backdrop-blur-xs">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white/15 text-amber-300">
                  <i class="mdi mdi-map-marker-radius text-xl" />
                </div>
                <div>
                  <p class="text-xs font-bold text-white">ปักหมุดบนแผนที่</p>
                  <p class="text-[11px] text-amber-100/70">ค้นหาง่าย นำทางสะดวก</p>
                </div>
              </div>

              <div class="flex items-center gap-3 rounded-2xl bg-white/5 border border-white/10 p-3.5 backdrop-blur-xs">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white/15 text-amber-300">
                  <i class="mdi mdi-package-variant-closed text-xl" />
                </div>
                <div>
                  <p class="text-xs font-bold text-white">ขายสินค้าออนไลน์</p>
                  <p class="text-[11px] text-amber-100/70">จัดการสต็อกและออเดอร์</p>
                </div>
              </div>

              <div class="flex items-center gap-3 rounded-2xl bg-white/5 border border-white/10 p-3.5 backdrop-blur-xs">
                <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white/15 text-amber-300">
                  <i class="mdi mdi-shield-check text-xl" />
                </div>
                <div>
                  <p class="text-xs font-bold text-white">ปลอดภัย โปร่งใส</p>
                  <p class="text-[11px] text-amber-100/70">โอนรายได้ตรงเข้าบัญชี</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- STATE 1: PENDING APPROVAL VIEW (SPACIOUS 2-COLUMN) -->
        <section
          v-if="shop?.status === 'PendingApproval' && !isEditingPending"
          class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start"
        >
          <!-- MAIN SUMMARY COLUMN -->
          <div class="lg:col-span-8 rounded-3xl border border-amber-200 bg-white p-6 sm:p-10 shadow-xs space-y-8">
            <div class="flex items-start gap-4 border-b border-[#F1E6D8] pb-6">
              <div class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-amber-50 text-amber-600 border border-amber-200 shadow-xs">
                <i class="mdi mdi-clock-check-outline text-3xl animate-pulse" />
              </div>
              <div>
                <span class="inline-flex items-center gap-1 rounded-full bg-amber-100 px-3 py-0.5 text-xs font-bold text-amber-800 mb-1.5">
                  อยู่ระหว่างการตรวจสอบ
                </span>
                <h2 class="text-xl sm:text-2xl font-bold text-[#332820]">
                  ส่งใบสมัครเปิดร้านค้าเรียบร้อยแล้ว
                </h2>
                <p class="mt-1 text-sm text-[#786B62]">
                  ร้านค้า <span class="font-bold text-[#332820]">"{{ shop.shopName }}"</span> อยู่ระหว่างการพิจารณาตรวจสอบข้อมูลจากเจ้าหน้าที่
                </p>
              </div>
            </div>

            <!-- STEPPER PROGRESS TIMELINE -->
            <div class="py-4">
              <p class="text-xs font-bold uppercase tracking-wider text-[#A89B90] mb-4">สถานะขั้นตอนการดำเนินงาน</p>
              <div class="grid grid-cols-3 gap-4 text-center">
                <!-- Step 1 -->
                <div class="rounded-2xl border border-emerald-200 bg-emerald-50/50 p-4 space-y-1.5">
                  <div class="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-emerald-600 text-white font-bold text-sm shadow-xs">
                    <i class="mdi mdi-check text-lg" />
                  </div>
                  <p class="text-xs font-bold text-emerald-900">1. ยื่นใบสมัคร</p>
                  <p class="text-[11px] text-emerald-700">ส่งข้อมูลเรียบร้อย</p>
                </div>

                <!-- Step 2 -->
                <div class="rounded-2xl border-2 border-amber-400 bg-amber-50/80 p-4 space-y-1.5 shadow-sm">
                  <div class="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-amber-500 text-white font-bold text-sm shadow-xs animate-bounce">
                    <i class="mdi mdi-magnify text-lg" />
                  </div>
                  <p class="text-xs font-bold text-amber-900">2. กำลังตรวจสอบ</p>
                  <p class="text-[11px] text-amber-700 font-semibold">เจ้าหน้าที่พิจารณา</p>
                </div>

                <!-- Step 3 -->
                <div class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4 space-y-1.5 opacity-70">
                  <div class="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-[#E8D9C9] text-[#786B62] font-bold text-sm">
                    <i class="mdi mdi-store-check text-lg" />
                  </div>
                  <p class="text-xs font-bold text-[#786B62]">3. เปิดใช้งานร้าน</p>
                  <p class="text-[11px] text-[#A89B90]">รอผลอนุมัติ</p>
                </div>
              </div>
            </div>

            <!-- SUBMITTED APPLICATION DETAILS -->
            <div class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-6 space-y-4">
              <h3 class="font-bold text-[#332820] text-sm flex items-center gap-2">
                <i class="mdi mdi-file-document-outline text-[#D96C2C] text-base" />
                <span>ข้อมูลที่คุณส่งมา</span>
              </h3>

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">ชื่อร้านค้า</span>
                  <span class="font-bold text-[#332820] text-sm">{{ shop.shopName }}</span>
                </div>

                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">หมวดหมู่ร้านค้า</span>
                  <span class="font-bold text-[#332820] text-sm">{{ shop.categoryName || getSelectedCategoryName() || '-' }}</span>
                </div>

                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">ที่ตั้ง (อำเภอ / ตำบล)</span>
                  <span class="font-semibold text-[#332820]">
                    {{ shop.districtName || getSelectedDistrictName() || '-' }} / {{ shop.subDistrictName || getSelectedSubDistrictName() || '-' }}
                  </span>
                </div>

                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">เบอร์ติดต่อ / อีเมล</span>
                  <span class="font-semibold text-[#332820]">
                    {{ shop.phone || '-' }} | {{ shop.email || '-' }}
                  </span>
                </div>

                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">บัญชีธนาคารรับเงิน</span>
                  <span class="font-semibold text-[#332820]">{{ shop.bankName || '-' }}</span>
                </div>

                <div class="rounded-xl border border-[#E8D9C9] bg-white p-3.5">
                  <span class="text-[#A89B90] block mb-1">ชื่อบัญชีธนาคาร</span>
                  <span class="font-semibold text-[#332820]">{{ shop.bankAccountName || '-' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- SIDEBAR ACTIONS & SUPPORT -->
          <div class="lg:col-span-4 space-y-6">
            <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-xs space-y-4">
              <h3 class="font-bold text-[#332820] text-sm">การดำเนินการ</h3>
              <p class="text-xs text-[#786B62]">
                หากพบว่าพิมพ์ข้อมูลผิดพลาด สามารถกดแก้ไขใบสมัครเพื่อส่งข้อมูลที่ถูกต้องใหม่ได้
              </p>

              <button
                type="button"
                class="w-full inline-flex items-center justify-center gap-2 rounded-xl border border-[#D9C5B0] bg-white py-3 text-xs sm:text-sm font-semibold text-[#4A3D34] shadow-xs hover:bg-[#FFF9F2] transition cursor-pointer"
                @click="isEditingPending = true"
              >
                <i class="mdi mdi-pencil-outline text-base text-[#D96C2C]" />
                <span>แก้ไขข้อมูลใบสมัคร</span>
              </button>

              <button
                type="button"
                class="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-[#332820] hover:bg-[#4A3D34] py-3 text-xs sm:text-sm font-semibold text-white shadow-xs transition cursor-pointer"
                @click="checkStatus"
              >
                <i class="mdi mdi-refresh text-base" />
                <span>ตรวจสอบสถานะล่าสุด</span>
              </button>
            </div>

            <!-- INFO CALLOUT -->
            <div class="rounded-3xl border border-[#F6D9C2] bg-[#FFF1E6] p-6 space-y-3 text-xs text-[#786B62]">
              <div class="flex items-center gap-2 font-bold text-[#7A3A12]">
                <i class="mdi mdi-information-outline text-lg text-[#D96C2C]" />
                <span>ระยะเวลาการพิจารณา</span>
              </div>
              <p class="leading-relaxed">
                เจ้าหน้าที่จะตรวจสอบข้อมูลและอนุมัติร้านค้าภายใน 1-2 วันทำการ เมื่อได้รับการอนุมัติแล้ว คุณจะสามารถเข้าสู่เมนู <span class="font-bold text-[#332820]">"สถานประกอบการ"</span> เพื่อเริ่มลงสินค้าและจัดการร้านค้าได้ทันที
              </p>
            </div>
          </div>
        </section>

        <!-- STATE 2: APPLICATION FORM (2-COLUMN WIDE LAYOUT) -->
        <div v-else class="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          <!-- MAIN FORM AREA (8 COLUMNS) -->
          <form class="lg:col-span-8 space-y-8" @submit.prevent="submit">
            <!-- REJECTED NOTICE -->
            <div
              v-if="shop?.status === 'Rejected'"
              class="rounded-3xl border border-rose-200 bg-rose-50 p-6 text-rose-800 shadow-xs flex items-start gap-4"
            >
              <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-rose-100 text-rose-600">
                <i class="mdi mdi-alert-circle text-2xl" />
              </div>
              <div>
                <h3 class="font-bold text-sm">ใบสมัครของคุณยังไม่ผ่านการอนุมัติ</h3>
                <p class="mt-1 text-xs text-rose-700 leading-relaxed">
                  กรุณาตรวจสอบความถูกต้องของข้อมูลร้านค้า หมวดหมู่ ที่ตั้ง หรือข้อมูลบัญชีธนาคารให้ครบถ้วน จากนั้นแก้ไขและส่งใบสมัครใหม่อีกครั้ง
                </p>
              </div>
            </div>

            <!-- EDITING PENDING NOTICE -->
            <div
              v-if="shop?.status === 'PendingApproval' && isEditingPending"
              class="rounded-2xl border border-amber-200 bg-amber-50 p-4 text-amber-800 shadow-xs flex items-center justify-between gap-3 text-xs"
            >
              <div class="flex items-center gap-2 font-medium">
                <i class="mdi mdi-information-outline text-base text-amber-600" />
                <span>คุณกำลังอยู่ในโหมดแก้ไขข้อมูลใบสมัครที่ส่งไปแล้ว</span>
              </div>
              <button
                type="button"
                class="font-bold underline hover:text-amber-900 cursor-pointer"
                @click="isEditingPending = false"
              >
                ยกเลิกการแก้ไข
              </button>
            </div>

            <!-- SECTION 1: STORE PROFILE -->
            <section
              id="section-1"
              class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6"
            >
              <div class="flex items-center justify-between border-b border-[#F1E6D8] pb-4">
                <div class="flex items-center gap-3.5">
                  <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#FFF1E6] text-[#BF5720] font-bold text-sm">
                    01
                  </span>
                  <div>
                    <h2 class="text-base sm:text-lg font-bold text-[#332820]">
                      ข้อมูลร้านของคุณ
                    </h2>
                    <p class="text-xs text-[#786B62]">ชื่อและประเภทร้านที่ลูกค้าจะเห็น</p>
                  </div>
                </div>

                <span
                  v-if="isSection1Complete"
                  class="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700 border border-emerald-200"
                >
                  <i class="mdi mdi-check text-xs" /> ข้อมูลครบ
                </span>
              </div>

              <div class="space-y-5">
                <!-- Shop Name -->
                <AppTextField
                  v-model="form.shopName"
                  label="ชื่อร้าน * (จำเป็น)"
                  placeholder="เช่น ร้านของฝากเมืองกาญจน์, คาเฟ่ริมแคว"
                />

                <!-- Category & Phone -->
                <div class="grid items-end gap-5 sm:grid-cols-2">
                  <AppSelect
                    v-model="form.shopCategoryId"
                    label="หมวดหมู่ร้านค้า * (จำเป็น)"
                    :items="categories"
                    item-title="categoryName"
                    item-value="shopCategoryId"
                    placeholder="เลือกหมวดหมู่ร้านค้า"
                    clearable
                  />

                  <AppTextField
                    v-model="form.phone"
                    label="เบอร์โทรศัพท์ (ไม่บังคับ)"
                    placeholder="เช่น 0812345678"
                  />
                </div>

                <!-- Email & Hours -->
                <div class="grid items-end gap-5 sm:grid-cols-2">
                  <AppTextField
                    v-model="form.email"
                    label="อีเมล (ไม่บังคับ)"
                    type="email"
                    placeholder="shop@example.com"
                  />

                  <div>
                    <label class="block text-xs font-semibold text-[#4A3D34] mb-1.5">
                      เวลาเปิด - ปิดร้าน (ไม่บังคับ)
                    </label>
                    <div class="grid grid-cols-2 gap-2">
                      <AppTextField
                        v-model="form.openingTime"
                        type="time"
                        aria-label="เวลาเปิด"
                      />
                      <AppTextField
                        v-model="form.closingTime"
                        type="time"
                        aria-label="เวลาปิด"
                      />
                    </div>
                  </div>
                </div>

                <!-- Quick Hours Preset Chips -->
                <div class="rounded-2xl border border-[#F1E6D8] bg-[#FFF9F2] p-4">
                  <p class="text-xs font-semibold text-[#786B62] mb-2">
                    ไม่อยากกรอกเอง? แตะเลือกได้เลย:
                  </p>
                  <div class="flex flex-wrap gap-2">
                    <button
                      v-for="hrs in popularHours"
                      :key="hrs.label"
                      type="button"
                      class="rounded-xl border border-[#E8D9C9] bg-white px-3 py-1.5 text-xs font-medium text-[#4A3D34] hover:border-[#D96C2C] hover:bg-[#FFF1E6] hover:text-[#7A3A12] transition cursor-pointer shadow-2xs"
                      @click="applyOperatingHours(hrs.open, hrs.close)"
                    >
                      {{ hrs.label }}
                    </button>
                  </div>
                </div>

                <!-- Description -->
                <AppTextarea
                  v-model="form.description"
                  label="เล่าให้ลูกค้ารู้จักร้านของคุณ (ไม่บังคับ)"
                  placeholder="เช่น ขายขนมไทยทำสดทุกเช้า สูตรของคุณยาย"
                />
              </div>
            </section>

            <!-- SECTION 2: LOCATION & MAP -->
            <section
              id="section-2"
              class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6"
            >
              <div class="flex items-center justify-between border-b border-[#F1E6D8] pb-4">
                <div class="flex items-center gap-3.5">
                  <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#FFF1E6] text-[#BF5720] font-bold text-sm">
                    02
                  </span>
                  <div>
                    <h2 class="text-base sm:text-lg font-bold text-[#332820]">
                      ร้านอยู่ที่ไหน
                    </h2>
                    <p class="text-xs text-[#786B62]">เลือกอำเภอ ตำบล แล้วปักหมุดบนแผนที่ให้ลูกค้าหาร้านเจอ</p>
                  </div>
                </div>

                <span
                  v-if="isSection2Complete"
                  class="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700 border border-emerald-200"
                >
                  <i class="mdi mdi-check text-xs" /> ข้อมูลครบ
                </span>
              </div>

              <div class="space-y-5">
                <!-- District & SubDistrict -->
                <div class="grid items-end gap-5 sm:grid-cols-2">
                  <AppSelect
                    v-model="form.districtId"
                    label="อำเภอ * (จำเป็น)"
                    :items="districts"
                    item-title="districtName"
                    item-value="districtId"
                    placeholder="เลือกอำเภอ"
                    clearable
                  />

                  <AppSelect
                    v-model="form.subDistrictId"
                    label="ตำบล * (จำเป็น)"
                    :items="subDistricts"
                    item-title="subDistrictName"
                    item-value="subDistrictId"
                    placeholder="เลือกตำบล"
                    :disabled="!form.districtId"
                    clearable
                  />
                </div>

                <!-- Detailed Address -->
                <AppTextarea
                  v-model="form.address"
                  label="ที่อยู่ร้าน (ไม่บังคับ)"
                  placeholder="บ้านเลขที่, หมู่ที่, ถนน, ซอย หรือจุดสังเกตสำคัญใกล้เคียง"
                />

                <!-- Map Picker (Spacious height) -->
                <div class="pt-2 space-y-2.5">
                  <div class="flex items-center justify-between text-xs">
                    <span class="font-bold text-[#332820] flex items-center gap-1.5">
                      <i class="mdi mdi-map-marker text-[#D96C2C] text-sm" />
                      ปักหมุดตำแหน่งร้านบนแผนที่
                    </span>
                    <span v-if="form.latitude && form.longitude" class="text-[#BF5720] font-mono font-semibold bg-[#FFF1E6] px-2 py-0.5 rounded">
                      พิกัด: {{ form.latitude.toFixed(4) }}, {{ form.longitude.toFixed(4) }}
                    </span>
                  </div>

                  <div class="rounded-2xl border border-[#D9C5B0] overflow-hidden shadow-xs h-[360px]">
                    <LocationPickerMap
                      v-model:latitude="form.latitude"
                      v-model:longitude="form.longitude"
                      @address-detected="onAddressDetected"
                    />
                  </div>
                  <p class="text-[11px] text-[#786B62]">
                    วิธีใช้: แตะบนแผนที่ตรงที่ตั้งร้าน หรือลากหมุดเพื่อขยับตำแหน่ง
                  </p>
                </div>
              </div>
            </section>

            <!-- SECTION 3: BANKING & PAYOUT -->
            <section
              id="section-3"
              class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6"
            >
              <div class="flex items-center justify-between border-b border-[#F1E6D8] pb-4">
                <div class="flex items-center gap-3.5">
                  <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-[#FFF1E6] text-[#BF5720] font-bold text-sm">
                    03
                  </span>
                  <div>
                    <h2 class="text-base sm:text-lg font-bold text-[#332820]">
                      รับเงินเข้าบัญชีไหน (กรอกทีหลังได้)
                    </h2>
                    <p class="text-xs text-[#786B62]">ไม่ต้องกรอกตอนนี้ก็ได้ แต่ต้องกรอกก่อนเริ่มขายสินค้า</p>
                  </div>
                </div>

                <span
                  v-if="isSection3Complete"
                  class="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2.5 py-0.5 text-xs font-semibold text-emerald-700 border border-emerald-200"
                >
                  <i class="mdi mdi-check text-xs" /> ข้อมูลครบ
                </span>
              </div>

              <div class="space-y-5">
                <!-- Quick Select Bank Chips -->
                <div>
                  <label class="block text-xs font-semibold text-[#4A3D34] mb-2">
                    แตะเลือกธนาคารของคุณ:
                  </label>
                  <div class="flex flex-wrap gap-2">
                    <button
                      v-for="b in popularBanks"
                      :key="b"
                      type="button"
                      class="rounded-xl border px-3.5 py-2 text-xs font-semibold transition cursor-pointer"
                      :class="
                        form.bankName === b
                          ? 'border-[#D96C2C] bg-[#FFF1E6] text-[#BF5720] ring-2 ring-[#D96C2C]/20 font-bold shadow-xs'
                          : 'border-[#E8D9C9] bg-white text-[#4A3D34] hover:bg-[#FFF9F2] hover:border-[#D9C5B0]'
                      "
                      @click="selectBank(b)"
                    >
                      {{ b }}
                    </button>
                  </div>
                </div>

                <!-- Bank Name & Account Name -->
                <div class="grid items-end gap-5 sm:grid-cols-2">
                  <AppTextField
                    v-model="form.bankName"
                    label="ธนาคาร"
                    placeholder="เช่น ธนาคารกสิกรไทย, ธนาคารกรุงไทย"
                  />

                  <AppTextField
                    v-model="form.bankAccountName"
                    label="ชื่อเจ้าของบัญชี"
                    placeholder="ระบุชื่อ-นามสกุลเจ้าของบัญชี"
                  />
                </div>

                <!-- Account Number & PromptPay -->
                <div class="grid items-end gap-5 sm:grid-cols-2">
                  <AppTextField
                    v-model="form.bankAccountNumber"
                    label="เลขบัญชี"
                    placeholder="เช่น 123-4-56789-0"
                  />

                  <AppTextField
                    v-model="form.promptPay"
                    label="เบอร์พร้อมเพย์ (ถ้ามี)"
                    placeholder="เช่น 0812345678 หรือเลขประจำตัวประชาชน"
                  />
                </div>
              </div>
            </section>

            <!-- SUBMIT BUTTON CARD -->
            <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-4">
              <div class="flex items-start gap-3 text-xs text-[#786B62]">
                <i class="mdi mdi-shield-check-outline text-lg text-emerald-600 shrink-0" />
                <span class="leading-relaxed">
                  เมื่อกดส่งใบสมัคร ข้อมูลร้านค้าจะถูกส่งไปยังผู้ดูแลระบบเพื่อตรวจสอบความถูกต้อง เจ้าหน้าที่จะใช้เวลาพิจารณาอนุมัติภายใน 1-2 วันทำการ
                </span>
              </div>

              <button
                type="submit"
                class="w-full inline-flex items-center justify-center gap-2 rounded-2xl bg-[#D96C2C] hover:bg-[#BF5720] px-8 py-4 text-base font-bold text-white shadow-lg shadow-[#D96C2C]/25 transition-all duration-200 active:scale-98 disabled:opacity-50 cursor-pointer"
                :disabled="saving"
              >
                <i
                  class="mdi text-lg"
                  :class="saving ? 'mdi-loading animate-spin' : 'mdi-send-outline'"
                />
                <span>
                  {{
                    saving
                      ? "กำลังส่งข้อมูลใบสมัคร..."
                      : shop?.status === "Rejected"
                        ? "แก้ไขและส่งใบสมัครอีกครั้ง"
                        : shop?.status === "PendingApproval"
                          ? "อัปเดตข้อมูลใบสมัคร"
                          : "ส่งใบสมัครเปิดร้านค้า"
                  }}
                </span>
              </button>
            </div>
          </form>

          <!-- STICKY SIDEBAR OVERVIEW (4 COLUMNS) -->
          <aside class="lg:col-span-4 space-y-6 lg:sticky lg:top-24">
            <!-- LIVE STORE CARD PREVIEW -->
            <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-xs space-y-4">
              <div class="flex items-center justify-between border-b border-[#F1E6D8] pb-3">
                <span class="text-xs font-bold text-[#786B62] uppercase tracking-wider">
                  ร้านของคุณจะหน้าตาแบบนี้
                </span>
                <span class="rounded-full bg-[#F1E6D8] px-2 py-0.5 text-[10px] font-bold text-[#786B62]">
                  ตัวอย่าง
                </span>
              </div>

              <!-- PREVIEW CARD CONTENT -->
              <div class="rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4 space-y-3">
                <div class="flex items-center gap-3">
                  <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-[#FFF1E6] border border-[#F6D9C2] text-[#BF5720]">
                    <i class="mdi mdi-storefront text-2xl" />
                  </div>

                  <div class="min-w-0">
                    <p class="font-bold text-sm text-[#332820] truncate">
                      {{ form.shopName || "ชื่อร้านค้าของคุณ" }}
                    </p>
                    <span
                      class="inline-block mt-0.5 rounded-md px-2 py-0.5 text-[11px] font-semibold"
                      :class="getSelectedCategoryName() ? 'bg-[#FFF1E6] text-[#BF5720] border border-[#F6D9C2]' : 'bg-[#E8D9C9] text-[#786B62]'"
                    >
                      {{ getSelectedCategoryName() || "หมวดหมู่ธุรกิจ" }}
                    </span>
                  </div>
                </div>

                <div class="space-y-1.5 pt-2 border-t border-[#E8D9C9] text-xs text-[#786B62]">
                  <div class="flex items-center gap-1.5">
                    <i class="mdi mdi-map-marker text-[#A89B90]" />
                    <span class="truncate">
                      {{ getSelectedDistrictName() ? `${getSelectedDistrictName()} / ${getSelectedSubDistrictName() || 'เลือกตำบล'}` : 'ระบุที่ตั้งอำเภอและตำบล' }}
                    </span>
                  </div>

                  <div class="flex items-center gap-1.5">
                    <i class="mdi mdi-clock-outline text-[#A89B90]" />
                    <span>
                      {{ form.openingTime && form.closingTime ? `${form.openingTime} - ${form.closingTime} น.` : 'ระบุเวลาเปิด-ปิด' }}
                    </span>
                  </div>

                  <div v-if="form.phone" class="flex items-center gap-1.5 text-[#4A3D34] font-semibold">
                    <i class="mdi mdi-phone text-[#A89B90]" />
                    <span>{{ form.phone }}</span>
                  </div>
                </div>
              </div>
            </div>

            <!-- APPLICATION PROGRESS & QUICK JUMP -->
            <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-xs space-y-4">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-[#4A3D34] uppercase tracking-wider">
                  กรอกไปแล้ว
                </span>
                <span class="font-bold text-[#BF5720] text-xs">
                  {{ completedSectionsCount }} / 3 ส่วน
                </span>
              </div>

              <!-- PROGRESS BAR -->
              <div class="h-2 w-full rounded-full bg-[#F1E6D8] overflow-hidden">
                <div
                  class="h-full bg-[#D96C2C] transition-all duration-300 rounded-full"
                  :style="{ width: `${(completedSectionsCount / 3) * 100}%` }"
                />
              </div>

              <!-- CHECKLIST NAVIGATION -->
              <div class="space-y-2 pt-2">
                <button
                  type="button"
                  class="w-full flex items-center justify-between p-2.5 rounded-xl border text-xs font-medium transition cursor-pointer text-left"
                  :class="isSection1Complete ? 'border-emerald-200 bg-emerald-50/50 text-emerald-800' : 'border-[#E8D9C9] hover:bg-[#FFF9F2] text-[#4A3D34]'"
                  @click="scrollToSection('section-1')"
                >
                  <span class="flex items-center gap-2">
                    <span class="font-bold">01</span>
                    <span>ข้อมูลทั่วไปของร้านค้า</span>
                  </span>
                  <i :class="isSection1Complete ? 'mdi mdi-check-circle text-emerald-600 text-base' : 'mdi mdi-circle-outline text-[#D9C5B0]'" />
                </button>

                <button
                  type="button"
                  class="w-full flex items-center justify-between p-2.5 rounded-xl border text-xs font-medium transition cursor-pointer text-left"
                  :class="isSection2Complete ? 'border-emerald-200 bg-emerald-50/50 text-emerald-800' : 'border-[#E8D9C9] hover:bg-[#FFF9F2] text-[#4A3D34]'"
                  @click="scrollToSection('section-2')"
                >
                  <span class="flex items-center gap-2">
                    <span class="font-bold">02</span>
                    <span>ที่ตั้งและพิกัดแผนที่</span>
                  </span>
                  <i :class="isSection2Complete ? 'mdi mdi-check-circle text-emerald-600 text-base' : 'mdi mdi-circle-outline text-[#D9C5B0]'" />
                </button>

                <button
                  type="button"
                  class="w-full flex items-center justify-between p-2.5 rounded-xl border text-xs font-medium transition cursor-pointer text-left"
                  :class="isSection3Complete ? 'border-emerald-200 bg-emerald-50/50 text-emerald-800' : 'border-[#E8D9C9] hover:bg-[#FFF9F2] text-[#4A3D34]'"
                  @click="scrollToSection('section-3')"
                >
                  <span class="flex items-center gap-2">
                    <span class="font-bold">03</span>
                    <span>ข้อมูลบัญชีธนาคารรับเงิน</span>
                  </span>
                  <i :class="isSection3Complete ? 'mdi mdi-check-circle text-emerald-600 text-base' : 'mdi mdi-circle-outline text-[#D9C5B0]'" />
                </button>
              </div>

              <!-- QUICK SUBMIT BUTTON FROM SIDEBAR -->
              <button
                type="button"
                class="w-full inline-flex items-center justify-center gap-2 rounded-xl bg-[#D96C2C] hover:bg-[#BF5720] py-3 text-xs sm:text-sm font-bold text-white shadow-md shadow-[#D96C2C]/25 transition cursor-pointer disabled:opacity-50"
                :disabled="saving || !isFormValid"
                @click="submit"
              >
                <i :class="saving ? 'mdi mdi-loading animate-spin' : 'mdi mdi-send-outline'" />
                <span>{{ saving ? 'กำลังส่งข้อมูล...' : 'ส่งใบสมัครเปิดร้าน' }}</span>
              </button>
            </div>

            <!-- HELP & FAQ CALLOUT -->
            <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 shadow-xs space-y-3 text-xs text-[#786B62]">
              <h4 class="font-bold text-[#332820] flex items-center gap-1.5">
                <i class="mdi mdi-help-circle-outline text-[#D96C2C] text-base" />
                <span>ต้องการความช่วยเหลือ?</span>
              </h4>
              <p class="leading-relaxed text-[#786B62]">
                หากมีข้อสงสัยเกี่ยวกับเงื่อนไขการสมัคร หรือต้องการความช่วยเหลือในการปักหมุดร้านค้า สามารถติดต่อเจ้าหน้าที่ผู้ดูแลระบบได้
              </p>
              <div class="pt-2 border-t border-[#F1E6D8] flex items-center gap-2 text-[#BF5720] font-semibold">
                <i class="mdi mdi-phone" />
                <span>ฝ่ายสนับสนุนร้านค้า: 034-000-000</span>
              </div>
            </div>
          </aside>
        </div>
      </template>
    </div>
  </main>
</template>

<style scoped>
.shop-apply :deep(input),
.shop-apply :deep(textarea) {
  border-color: #d9c5b0;
  background-color: #fff;
  color: #332820;
  font-size: 0.95rem;
}
.shop-apply :deep(input::placeholder),
.shop-apply :deep(textarea::placeholder) {
  color: #a89b90;
}
.shop-apply :deep(input:focus),
.shop-apply :deep(textarea:focus) {
  border-color: #d96c2c;
  box-shadow: 0 0 0 3px rgb(217 108 44 / 0.18);
}
.shop-apply :deep(label > span:first-child) {
  color: #4a3d34;
  font-weight: 700;
}
</style>

<style scoped>
/* ให้ป้ายชื่อและความสูงของช่องเลือก/ช่องกรอกเท่ากัน เพื่อให้อยู่แถวเดียวกันตรงกัน */
.shop-apply :deep(input:not([type='checkbox'])) {
  height: 46px;
  padding-top: 0;
  padding-bottom: 0;
}
.shop-apply :deep(label.block.text-xs),
.shop-apply :deep(label > span:first-child) {
  display: block;
  font-size: 0.875rem;
  line-height: 1.25rem;
  font-weight: 700;
  color: #4a3d34;
}
.shop-apply :deep(.min-h-\[44px\]) {
  min-height: 46px;
  padding-top: 0;
  padding-bottom: 0;
  font-size: 0.95rem;
}
</style>
