<script setup lang="ts">
import axios from 'axios'
import { onMounted, ref, watch } from 'vue'
import {
  closeShop,
  createShop,
  getDistricts,
  getMyShop,
  getShopCategories,
  getSubDistricts,
  updateShop,
  updateShopStatus,
  uploadShopBackgroundImage,
  uploadShopCoverImage,
  type District,
  type Shop,
  type ShopCategory,
  type ShopFormData,
  type SubDistrict,
} from '@/features/merchant/api/shopApi'
import AppSelect from '@/components/common/input/AppSelect.vue'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'
import LocationPickerMap from '@/components/common/map/LocationPickerMap.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { imageUrl } from '@/shared/utils/imageUrl'

const shop = ref<Shop | null>(null)
const categories = ref<ShopCategory[]>([])
const districts = ref<District[]>([])
const subDistricts = ref<SubDistrict[]>([])
const loading = ref(true)
const saving = ref(false)
const uploadingCover = ref(false)
const uploadingBackground = ref(false)
const restoringLocation = ref(false)
const swal = useSwal()
const activeTab = ref<'general' | 'contact' | 'bank' | 'location'>('general')

watch(activeTab, (tab) => {
  if (tab === 'location') {
    setTimeout(() => {
      window.dispatchEvent(new Event('resize'))
    }, 150)
  }
})


const form = ref<ShopFormData>({
  shopName: '',
  shopCategoryId: null,
  districtId: null,
  subDistrictId: null,
  description: '',
  phone: '',
  email: '',
  address: '',
  openingTime: '',
  closingTime: '',
  latitude: null,
  longitude: null,
  bankName: '',
  bankAccountName: '',
  bankAccountNumber: '',
  promptPay: '',
})

function applyShop(data: Shop) {
  shop.value = data
  form.value = {
    shopName: data.shopName,
    shopCategoryId: data.shopCategoryId,
    districtId: data.districtId,
    subDistrictId: data.subDistrictId,
    description: data.description ?? '',
    phone: data.phone ?? '',
    email: data.email ?? '',
    address: data.address ?? '',
    openingTime: data.openingTime ?? '',
    closingTime: data.closingTime ?? '',
    latitude: data.latitude ?? null,
    longitude: data.longitude ?? null,
    bankName: data.bankName ?? '',
    bankAccountName: data.bankAccountName ?? '',
    bankAccountNumber: data.bankAccountNumber ?? '',
    promptPay: data.promptPay ?? '',
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

watch(
  () => form.value.districtId,
  (value, oldValue) => {
    if (!restoringLocation.value && value && value !== oldValue) void loadSubDistricts(value)
  },
)

async function onAddressDetected(data: { districtName?: string; subDistrictName?: string }) {
  if (!data.districtName) return
  const targetDistName = data.districtName.replace(/^(อำเภอ|อ\.)\s*/, '').trim()

  const matchedDistrict = districts.value.find((d) => {
    const dName = d.districtName.replace(/^(อำเภอ|อ\.)\s*/, '').trim()
    return dName.includes(targetDistName) || targetDistName.includes(dName)
  })

  if (matchedDistrict) {
    form.value.districtId = matchedDistrict.districtId
    await loadSubDistricts(matchedDistrict.districtId, true)

    if (data.subDistrictName) {
      const targetSubName = data.subDistrictName.replace(/^(ตำบล|ต\.)\s*/, '').trim()
      const matchedSub = subDistricts.value.find((s) => {
        const sName = s.subDistrictName.replace(/^(ตำบล|ต\.)\s*/, '').trim()
        return sName.includes(targetSubName) || targetSubName.includes(sName)
      })

      if (matchedSub) {
        form.value.subDistrictId = matchedSub.subDistrictId
      }
    }
  }
}

async function submit() {
  if (
    !form.value.shopName ||
    !form.value.shopCategoryId ||
    !form.value.districtId ||
    !form.value.subDistrictId
  ) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณากรอกชื่อร้าน หมวดหมู่ เขต และตำบล ให้ครบถ้วน')
    return
  }
  saving.value = true
  try {
    if (shop.value) {
      await updateShop(shop.value.shopId, form.value)
      await swal.success('บันทึกข้อมูลร้านแล้ว', 'ข้อมูลร้านค้าของคุณได้รับการอัปเดตเรียบร้อยแล้ว')
    } else {
      const createdShop = await createShop(form.value)
      restoringLocation.value = true
      applyShop(createdShop)
      await loadSubDistricts(createdShop.districtId, true)
      restoringLocation.value = false
      await swal.success('สร้างร้านสำเร็จ', 'ขอยินดีต้อนรับสู่แพลตฟอร์มร้านค้าเมืองกาญจน์!')
    }
  } catch (error) {
    await swal.error('บันทึกร้านไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

async function removeShop() {
  if (!shop.value) return
  const result = await swal.confirm(
    'ยืนยันปิดบริการร้านนี้?',
    'ร้านจะไม่แสดงต่อสาธารณะบนแพลตฟอร์ม แต่ข้อมูลของคุณจะไม่ถูกลบ',
  )
  if (!result.isConfirmed) return
  try {
    await closeShop(shop.value.shopId)
    shop.value.status = 'Closed'
    await swal.success('ปิดบริการร้านค้าแล้ว')
  } catch (error) {
    await swal.error('ปิดร้านไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function reopenShop() {
  if (!shop.value) return
  const result = await swal.confirm(
    'เปิดบริการร้านค้าอีกครั้ง?',
    'ร้านจะแสดงต่อสาธารณะและสามารถรับคำสั่งซื้อได้อีกครั้ง',
  )
  if (!result.isConfirmed) return
  try {
    await updateShopStatus(shop.value.shopId, 'Active')
    shop.value.status = 'Active'
    await swal.success('เปิดบริการร้านค้าเรียบร้อยแล้ว')
  } catch (error) {
    await swal.error('เปิดร้านไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function uploadCoverImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !shop.value) return

  uploadingCover.value = true
  try {
    shop.value = await uploadShopCoverImage(shop.value.shopId, file)
    await swal.success('อัปโหลดรูปหน้าปกร้านแล้ว')
  } catch (error) {
    await swal.error(
      'อัปโหลดรูปไม่สำเร็จ',
      getApiErrorMessage(error, 'รองรับไฟล์ภาพ JPG, PNG, WEBP ขนาดไม่เกิน 5 MB'),
    )
  } finally {
    input.value = ''
    uploadingCover.value = false
  }
}

async function uploadBackgroundImage(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !shop.value) return

  uploadingBackground.value = true
  try {
    shop.value = await uploadShopBackgroundImage(shop.value.shopId, file)
    await swal.success('อัปโหลดรูปพื้นหลังร้านแล้ว')
  } catch (error) {
    await swal.error(
      'อัปโหลดรูปไม่สำเร็จ',
      getApiErrorMessage(error, 'รองรับไฟล์ภาพ JPG, PNG, WEBP ขนาดไม่เกิน 5 MB'),
    )
  } finally {
    input.value = ''
    uploadingBackground.value = false
  }
}

onMounted(async () => {
  try {
    ;[categories.value, districts.value] = await Promise.all([getShopCategories(), getDistricts()])
    try {
      const myShop = await getMyShop()
      restoringLocation.value = true
      applyShop(myShop)
      await loadSubDistricts(myShop.districtId, true)
      restoringLocation.value = false
    } catch (error) {
      if (!axios.isAxiosError(error) || error.response?.status !== 404) throw error
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-12 text-slate-800">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-28 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
      <div class="h-96 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
    </div>

    <template v-else>
      <!-- Page Header & Status Card -->
      <div
        class="flex flex-col justify-between gap-5 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs sm:flex-row sm:items-center lg:px-8 lg:py-6"
      >
        <div class="flex items-center gap-4">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-600 text-white shadow-xs shrink-0"
          >
            <i class="mdi mdi-store-cog-outline text-2xl text-white"></i>
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-1">
              <i class="mdi mdi-check-decagram text-xs text-emerald-600"></i>
              <span>ศูนย์จัดการร้านค้า • ข้อมูลสถานประกอบการ</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
              {{ shop ? 'ข้อมูลและการตั้งค่าร้านค้า' : 'ลงทะเบียนเปิดร้านค้าใหม่' }}
            </h1>
            <p class="text-xs sm:text-sm text-slate-600 font-medium mt-0.5">
              จัดการข้อมูลโปรไฟล์ รูปภาพหน้าร้าน เวลาทำการ บัญชีรับเงิน และตำแหน่งแผนที่ GPS
            </p>
          </div>
        </div>

        <div v-if="shop" class="flex flex-wrap items-center gap-3">
          <span
            class="inline-flex items-center gap-1.5 rounded-full px-3.5 py-1.5 text-xs font-bold border"
            :class="
              shop.status === 'Active'
                ? 'bg-emerald-50 text-emerald-800 border-emerald-300'
                : 'bg-amber-50 text-amber-800 border-amber-300'
            "
          >
            <span
              class="h-2 w-2 rounded-full"
              :class="shop.status === 'Active' ? 'bg-emerald-600 animate-pulse' : 'bg-amber-500'"
            ></span>
            {{ shop.status === 'Active' ? 'เปิดบริการต่อสาธารณะ' : 'ปิดบริการชั่วคราว' }}
          </span>

          <RouterLink
            :to="`/shops/${shop.shopId}`"
            target="_blank"
            class="inline-flex items-center gap-1.5 rounded-xl border border-slate-300 bg-white hover:bg-slate-50 px-4 py-2 text-xs font-bold text-slate-800 transition shadow-2xs cursor-pointer"
          >
            <i class="mdi mdi-open-in-new text-sm text-emerald-600"></i>
            <span>ดูหน้าร้านจริง</span>
          </RouterLink>
        </div>
      </div>

      <!-- Quick Navigation Tabs (High Contrast Active Pill) -->
      <div class="flex flex-wrap items-center gap-2 p-1.5 rounded-2xl bg-white border border-slate-200/90 shadow-xs">
        <button
          type="button"
          @click="activeTab = 'general'"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none select-none"
          :class="
            activeTab === 'general'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 font-semibold'
          "
        >
          <i class="mdi mdi-store-outline text-base"></i>
          <span>ข้อมูลทั่วไปและรูปภาพ</span>
        </button>

        <button
          type="button"
          @click="activeTab = 'contact'"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none select-none"
          :class="
            activeTab === 'contact'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 font-semibold'
          "
        >
          <i class="mdi mdi-clock-time-four-outline text-base"></i>
          <span>เวลาและช่องทางติดต่อ</span>
        </button>

        <button
          type="button"
          @click="activeTab = 'bank'"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none select-none"
          :class="
            activeTab === 'bank'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 font-semibold'
          "
        >
          <i class="mdi mdi-bank-outline text-base"></i>
          <span>บัญชีรับเงินยอดขาย</span>
        </button>

        <button
          type="button"
          @click="activeTab = 'location'"
          class="inline-flex items-center gap-2 px-4 py-2.5 rounded-xl text-xs sm:text-sm font-bold transition-all duration-200 cursor-pointer outline-none focus:outline-none select-none"
          :class="
            activeTab === 'location'
              ? 'bg-emerald-600 text-white shadow-xs'
              : 'text-slate-600 hover:text-slate-900 hover:bg-slate-100 font-semibold'
          "
        >
          <i class="mdi mdi-map-marker-radius-outline text-base"></i>
          <span>ที่อยู่และแผนที่ปักหมุด</span>
        </button>
      </div>

      <!-- Main Form Container -->
      <form class="space-y-6" @submit.prevent="submit">
        <!-- TAB 1: General Info & Images -->
        <div v-show="activeTab === 'general'" class="space-y-6">
          <!-- Profile Card -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 sm:p-8 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                <i class="mdi mdi-text-box-outline text-xl"></i>
              </div>
              <div>
                <h2 class="text-base sm:text-lg font-bold text-slate-900">ข้อมูลพื้นฐานร้านค้า</h2>
                <p class="text-xs text-slate-500 font-medium">ระบุชื่อร้านค้า หมวดหมู่หลัก และคำบรรยายจุดเด่นสำหรับลูกค้า</p>
              </div>
            </div>

            <div class="grid gap-6 md:grid-cols-2">
              <AppTextField
                v-model="form.shopName"
                label="ชื่อร้านค้า *"
                placeholder="ระบุชื่อร้านค้าของคุณ"
              />
              <AppSelect
                v-model="form.shopCategoryId"
                label="หมวดหมู่ร้านค้า *"
                :items="categories"
                item-title="categoryName"
                item-value="shopCategoryId"
                placeholder="เลือกหมวดหมู่ร้าน"
                clearable
              />
            </div>

            <AppTextarea
              v-model="form.description"
              label="รายละเอียดร้านค้า"
              placeholder="อธิบายจุดเด่น สินค้าแนะนำ บริการ หรือประวัติความเป็นมาของร้านคุณ..."
            />
          </div>

          <!-- Media Images Card -->
          <div v-if="shop" class="rounded-2xl border border-slate-200/80 bg-white p-6 sm:p-8 shadow-xs space-y-6">
            <div class="flex items-center justify-between border-b border-slate-100 pb-4 flex-wrap gap-2">
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                  <i class="mdi mdi-image-multiple-outline text-xl"></i>
                </div>
                <div>
                  <h2 class="text-base sm:text-lg font-bold text-slate-900">รูปภาพประกอบร้านค้า (Media & Banners)</h2>
                  <p class="text-xs text-slate-500 font-medium">ภาพหน้าปกและภาพพื้นหลังที่แสดงบนหน้ารวมและหน้าร้านของคุณ</p>
                </div>
              </div>
              <span class="text-xs font-semibold text-slate-600 bg-slate-100 px-3 py-1 rounded-full border border-slate-200">
                รองรับ JPG, PNG, WEBP (สูงสุด 5 MB)
              </span>
            </div>

            <div class="grid gap-6 md:grid-cols-2">
              <!-- Cover Image Box -->
              <div class="rounded-2xl border border-slate-200/80 bg-slate-50/60 p-5 space-y-4 flex flex-col justify-between">
                <div>
                  <div class="flex items-center justify-between">
                    <h3 class="font-bold text-slate-900 flex items-center gap-2 text-sm sm:text-base">
                      <i class="mdi mdi-image-outline text-emerald-600"></i>
                      รูปหน้าปกร้านค้า (Cover Image)
                    </h3>
                    <span class="text-[11px] font-bold text-emerald-800 bg-emerald-100 px-2 py-0.5 rounded-md border border-emerald-200">
                      แนะนำ 1200 x 800 px
                    </span>
                  </div>
                  <p class="mt-1 text-xs text-slate-500 font-medium">
                    ภาพปกจะแสดงบนการ์ดร้านค้าในหน้ารวมร้านค้าและส่วนหัวของหน้าร้านคุณ
                  </p>
                </div>

                <div class="space-y-3">
                  <div class="relative aspect-16/9 w-full overflow-hidden rounded-xl bg-slate-900 border border-slate-200 shadow-2xs group">
                    <img
                      v-if="shop.coverImageUrl"
                      :src="imageUrl(shop.coverImageUrl)"
                      alt="รูปหน้าปกร้าน"
                      class="h-full w-full object-cover group-hover:scale-102 transition duration-300"
                    />
                    <div
                      v-else
                      class="flex h-full w-full items-center justify-center text-slate-400 bg-slate-800"
                    >
                      <i class="mdi mdi-storefront text-5xl text-emerald-500"></i>
                    </div>
                  </div>

                  <label
                    class="cursor-pointer inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 w-full py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-600 select-none"
                  >
                    <i
                      class="mdi text-base text-white"
                      :class="uploadingCover ? 'mdi-loading animate-spin' : 'mdi-camera'"
                    ></i>
                    <span class="text-white font-bold">{{
                      uploadingCover ? 'กำลังอัปโหลด...' : 'เลือกและเปลี่ยนรูปหน้าปก'
                    }}</span>
                    <input
                      class="sr-only"
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      :disabled="uploadingCover"
                      @change="uploadCoverImage"
                    />
                  </label>
                </div>
              </div>

              <!-- Background Banner Box -->
              <div class="rounded-2xl border border-slate-200/80 bg-slate-50/60 p-5 space-y-4 flex flex-col justify-between">
                <div>
                  <div class="flex items-center justify-between">
                    <h3 class="font-bold text-slate-900 flex items-center gap-2 text-sm sm:text-base">
                      <i class="mdi mdi-panorama-outline text-emerald-600"></i>
                      รูปพื้นหลังหน้าร้าน (Background Banner)
                    </h3>
                    <span class="text-[11px] font-bold text-emerald-800 bg-emerald-100 px-2 py-0.5 rounded-md border border-emerald-200">
                      แนะนำ 1920 x 600 px
                    </span>
                  </div>
                  <p class="mt-1 text-xs text-slate-500 font-medium">
                    แสดงเป็นภาพพื้นหลังส่วนหัวในหน้ารายละเอียดร้านของคุณบนแพลตฟอร์ม
                  </p>
                </div>

                <div class="space-y-3">
                  <div class="relative aspect-16/9 w-full overflow-hidden rounded-xl bg-slate-900 border border-slate-200 shadow-2xs group">
                    <img
                      v-if="shop.backgroundImageUrl"
                      :src="imageUrl(shop.backgroundImageUrl)"
                      alt="รูปพื้นหลังร้าน"
                      class="h-full w-full object-cover group-hover:scale-102 transition duration-300"
                    />
                    <div
                      v-else
                      class="flex h-full w-full items-center justify-center text-slate-400 bg-slate-800"
                    >
                      <i class="mdi mdi-panorama text-5xl text-emerald-400"></i>
                    </div>
                  </div>

                  <label
                    class="cursor-pointer inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 w-full py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 border border-emerald-600 select-none"
                  >
                    <i
                      class="mdi text-base text-white"
                      :class="uploadingBackground ? 'mdi-loading animate-spin' : 'mdi-image-plus'"
                    ></i>
                    <span class="text-white font-bold">{{
                      uploadingBackground ? 'กำลังอัปโหลด...' : 'เลือกและเปลี่ยนรูปพื้นหลัง'
                    }}</span>
                    <input
                      class="sr-only"
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      :disabled="uploadingBackground"
                      @change="uploadBackgroundImage"
                    />
                  </label>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 2: Contact & Operating Hours -->
        <div v-show="activeTab === 'contact'" class="space-y-6">
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 sm:p-8 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                <i class="mdi mdi-card-account-phone-outline text-xl"></i>
              </div>
              <div>
                <h2 class="text-base sm:text-lg font-bold text-slate-900">ช่องทางการติดต่อและเวลาเปิดบริการ</h2>
                <p class="text-xs text-slate-500 font-medium">ระบุหมายเลขโทรศัพท์ อีเมล และช่วงเวลาทำการของร้านค้า</p>
              </div>
            </div>

            <div class="grid gap-6 sm:grid-cols-2">
              <AppTextField
                v-model="form.phone"
                label="เบอร์โทรศัพท์ติดต่อ *"
                placeholder="เช่น 0812345678"
              />
              <AppTextField
                v-model="form.email"
                label="อีเมลติดต่อร้านค้า"
                type="email"
                placeholder="shop@example.com"
              />
            </div>

            <div class="grid gap-6 sm:grid-cols-2">
              <AppTextField v-model="form.openingTime" label="เวลาเปิดร้าน" type="time" />
              <AppTextField v-model="form.closingTime" label="เวลาปิดร้าน" type="time" />
            </div>

            <!-- Operating Hours Preview Card (Clean Emerald style) -->
            <div class="rounded-xl border border-emerald-200 bg-emerald-50/50 p-4 flex items-center justify-between gap-4">
              <div class="flex items-center gap-3">
                <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-emerald-100 text-emerald-800 shrink-0">
                  <i class="mdi mdi-clock-check-outline text-xl"></i>
                </div>
                <div>
                  <p class="text-xs font-bold text-emerald-950">ตัวอย่างเวลาทำการที่ลูกค้าเห็นบนหน้าร้าน</p>
                  <p class="text-xs text-emerald-800 font-semibold mt-0.5">
                    {{ form.openingTime && form.closingTime ? `เปิดบริการทุกวัน เวลา ${form.openingTime} - ${form.closingTime} น.` : 'ยังไม่ได้ระบุเวลาเปิด-ปิดร้าน' }}
                  </p>
                </div>
              </div>
              <span
                class="px-2.5 py-1 text-xs font-bold rounded-full border shrink-0"
                :class="form.openingTime && form.closingTime ? 'bg-emerald-100 text-emerald-800 border-emerald-300' : 'bg-slate-100 text-slate-600 border-slate-200'"
              >
                {{ form.openingTime && form.closingTime ? 'พร้อมแสดงผล' : 'รอการระบุ' }}
              </span>
            </div>
          </div>
        </div>

        <!-- TAB 3: Payout Banking Info (Clean Light High-Contrast Style) -->
        <div v-show="activeTab === 'bank'" class="space-y-6">
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 sm:p-8 shadow-xs space-y-6">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-slate-100 pb-4">
              <div class="flex items-center gap-3">
                <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                  <i class="mdi mdi-bank-outline text-xl"></i>
                </div>
                <div>
                  <h2 class="text-base sm:text-lg font-bold text-slate-900">ข้อมูลบัญชีธนาคารสำหรับรับเงินโอนยอดขาย</h2>
                  <p class="text-xs text-slate-500 font-medium">ระบุบัญชีธนาคารหรือพร้อมเพย์สำหรับให้แอดมินโอนยอดเงินคืนร้านค้า</p>
                </div>
              </div>
              <span class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-50 text-emerald-800 text-xs font-bold border border-emerald-200 self-start sm:self-auto">
                <i class="mdi mdi-shield-lock-outline"></i>
                <span>ข้อมูลความปลอดภัย</span>
              </span>
            </div>

            <div class="grid gap-6 lg:grid-cols-3 items-start">
              <!-- Left Form (2 cols) -->
              <div class="lg:col-span-2 space-y-5">
                <div class="grid gap-5 sm:grid-cols-2">
                  <AppTextField
                    v-model="form.bankName"
                    label="ชื่อธนาคาร"
                    placeholder="เช่น ธนาคารกสิกรไทย, ธนาคารไทยพาณิชย์"
                  />
                  <AppTextField
                    v-model="form.bankAccountName"
                    label="ชื่อเจ้าของบัญชี"
                    placeholder="เช่น นาย สมชาย ใจดี"
                  />
                </div>

                <div class="grid gap-5 sm:grid-cols-2">
                  <AppTextField
                    v-model="form.bankAccountNumber"
                    label="เลขที่บัญชีธนาคาร"
                    placeholder="เช่น 123-4-56789-0"
                  />
                  <AppTextField
                    v-model="form.promptPay"
                    label="เบอร์พร้อมเพย์ (PromptPay)"
                    placeholder="เช่น 0812345678 หรือเลขบัตรประชาชน"
                  />
                </div>

                <div class="rounded-xl border border-slate-200/80 bg-slate-50 p-4 text-xs text-slate-600 font-medium flex items-start gap-2.5">
                  <i class="mdi mdi-information-outline text-emerald-600 text-lg shrink-0 mt-0.5"></i>
                  <span>
                    เมื่อลูกค้าชำระเงินสำเร็จผ่านระบบ ยอดขายจะถูกบันทึกเข้าระบบการจ่ายเงินของร้าน และแอดมินจะดำเนินการตรวจสอบสลิปและโอนเงินเข้าบัญชีตามข้อมูลที่ระบุนี้
                  </span>
                </div>
              </div>

              <!-- Right: Clean High-Contrast Payout Summary Card -->
              <div class="rounded-2xl p-5 bg-emerald-50/70 border border-emerald-200/90 text-slate-900 shadow-2xs space-y-4">
                <div class="flex items-center justify-between border-b border-emerald-200/80 pb-3">
                  <div class="flex items-center gap-2">
                    <div class="flex h-8 w-8 items-center justify-center rounded-lg bg-emerald-600 text-white shrink-0">
                      <i class="mdi mdi-bank text-base"></i>
                    </div>
                    <div>
                      <span class="text-xs font-bold text-emerald-950">ตัวอย่างบัญชีรับเงิน</span>
                      <p class="text-[11px] text-emerald-700">ใช้สำหรับรับเงินยอดขาย</p>
                    </div>
                  </div>
                  <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-100 text-emerald-800 border border-emerald-300">
                    Active
                  </span>
                </div>

                <div class="space-y-3 text-xs">
                  <div class="flex justify-between items-center py-1 border-b border-emerald-100/80">
                    <span class="text-slate-500 font-medium">ธนาคาร:</span>
                    <span class="font-bold text-slate-900">{{ form.bankName || 'ยังไม่ระบุ' }}</span>
                  </div>
                  <div class="flex justify-between items-center py-1 border-b border-emerald-100/80">
                    <span class="text-slate-500 font-medium">เลขที่บัญชี:</span>
                    <span class="font-black text-slate-900 tracking-wider text-sm">{{ form.bankAccountNumber || 'ยังไม่ระบุ' }}</span>
                  </div>
                  <div class="flex justify-between items-center py-1 border-b border-emerald-100/80">
                    <span class="text-slate-500 font-medium">ชื่อบัญชี:</span>
                    <span class="font-bold text-slate-900">{{ form.bankAccountName || 'ยังไม่ระบุ' }}</span>
                  </div>
                  <div class="flex justify-between items-center py-1">
                    <span class="text-slate-500 font-medium">พร้อมเพย์:</span>
                    <span class="font-bold text-slate-900">{{ form.promptPay || 'ยังไม่ระบุ' }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- TAB 4: Location & Map -->
        <div v-show="activeTab === 'location'" class="space-y-6">
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 sm:p-8 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                <i class="mdi mdi-map-marker-radius-outline text-xl"></i>
              </div>
              <div>
                <h2 class="text-base sm:text-lg font-bold text-slate-900">ที่อยู่และตำแหน่งร้าน (พิกัด GPS)</h2>
                <p class="text-xs text-slate-500 font-medium">ระบุเขต อำเภอ ตำบล และปักหมุดตำแหน่งร้านบนแผนที่เพื่อนำทางลูกค้า</p>
              </div>
            </div>

            <div class="grid gap-6 sm:grid-cols-2">
              <AppSelect
                v-model="form.districtId"
                label="เขต / อำเภอ *"
                :items="districts"
                item-title="districtName"
                item-value="districtId"
                placeholder="เลือกเขต / อำเภอ"
                clearable
              />
              <AppSelect
                v-model="form.subDistrictId"
                label="ตำบล / แขวง *"
                :items="subDistricts"
                item-title="subDistrictName"
                item-value="subDistrictId"
                placeholder="เลือกตำบล / แขวง"
                clearable
                :disabled="!form.districtId"
              />
            </div>

            <AppTextarea
              v-model="form.address"
              label="ที่อยู่ตั้งร้านค้าอย่างละเอียด"
              placeholder="บ้านเลขที่ อาคาร ซอย ถนน จุดสังเกต..."
            />

            <!-- Interactive Map Section -->
            <section class="space-y-3 pt-2">
              <div class="flex items-center justify-between flex-wrap gap-2">
                <div>
                  <h3 class="font-bold text-slate-900 text-sm flex items-center gap-1.5">
                    <i class="mdi mdi-crosshairs-gps text-emerald-600"></i>
                    <span>ปักหมุดตำแหน่งร้านค้าบนแผนที่</span>
                  </h3>
                  <p class="text-xs text-slate-500 font-medium">
                    คลิกบนแผนที่หรือลากหมุดเพื่อระบุตำแหน่งที่แน่นอน หรือกดปุ่มค้นหาตำแหน่งปัจจุบัน
                  </p>
                </div>
                <div v-if="form.latitude && form.longitude" class="text-xs font-bold text-emerald-800 bg-emerald-50 px-3 py-1 rounded-full border border-emerald-200">
                  พิกัด: {{ form.latitude.toFixed(5) }}, {{ form.longitude.toFixed(5) }}
                </div>
              </div>

              <div class="rounded-2xl border border-slate-200 overflow-hidden shadow-2xs">
                <LocationPickerMap
                  v-model:latitude="form.latitude"
                  v-model:longitude="form.longitude"
                  @address-detected="onAddressDetected"
                />
              </div>
            </section>
          </div>
        </div>

        <!-- Sticky / Bottom Action Bar -->
        <div class="rounded-2xl border border-slate-200/80 bg-white p-5 sm:p-6 shadow-xs flex flex-wrap items-center justify-between gap-4">
          <div>
            <button
              v-if="shop && shop.status !== 'Closed'"
              type="button"
              class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold text-rose-700 border border-rose-200 bg-rose-50 hover:bg-rose-600 hover:text-white transition cursor-pointer select-none"
              @click="removeShop"
            >
              <i class="mdi mdi-store-off-outline text-base"></i>
              <span>ปิดบริการร้านค้าชั่วคราว</span>
            </button>

            <button
              v-if="shop?.status === 'Closed'"
              type="button"
              class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-xs font-bold text-emerald-800 border border-emerald-300 bg-emerald-50 hover:bg-emerald-600 hover:text-white transition cursor-pointer select-none"
              @click="reopenShop"
            >
              <i class="mdi mdi-store-check-outline text-base"></i>
              <span>เปิดบริการร้านค้าอีกครั้ง</span>
            </button>
          </div>

          <div class="flex items-center gap-3 ml-auto">
            <button
              type="submit"
              class="inline-flex items-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-7 py-3 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 disabled:opacity-60 cursor-pointer border border-emerald-600 select-none"
              :disabled="saving || shop?.status === 'Closed'"
            >
              <i
                class="mdi text-lg text-white"
                :class="saving ? 'mdi-loading animate-spin' : 'mdi-content-save-outline'"
              ></i>
              <span class="text-white font-bold">{{
                saving ? 'กำลังบันทึก...' : shop ? 'บันทึกการเปลี่ยนแปลง' : 'ยืนยันสร้างร้านค้า'
              }}</span>
            </button>
          </div>
        </div>
      </form>
    </template>
  </div>
</template>

<style scoped>
button {
  outline: none !important;
  -webkit-tap-highlight-color: transparent;
}
button:focus,
button:focus-visible {
  outline: none !important;
}
</style>
