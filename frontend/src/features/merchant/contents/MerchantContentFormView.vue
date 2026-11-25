<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  createShopContent,
  getContentCategories,
  getDistricts,
  getOwnedShopContent,
  getSubDistricts,
  updateMyContent,
  type ContentCategory,
  type District,
  type SubmissionStatus,
  type SubDistrict,
  type UserContentFormData,
} from '@/features/contents/api'
import { getMyShop, type Shop } from '@/features/merchant/api/shopApi'
import { getCategoryRule } from '@/features/contents/constants/categoryRules'
import { getMyContentSchedules } from '@/features/contents/api'
import { createSchedule, updateSchedule } from '@/features/admin/schedules/api/adminScheduleApi'
import { toNullableNumber, youtubeEmbedUrl, youtubeThumbnail } from '@/shared/utils/youtube'
import AppSelect from '@/components/common/input/AppSelect.vue'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'
import LocationPickerMap from '@/components/common/map/LocationPickerMap.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import type { ScheduleItemForm } from './interface/type'

const route = useRoute()
const router = useRouter()
const swal = useSwal()

const contentId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => Boolean(contentId.value))
const loading = ref(true)
const saving = ref(false)

const shop = ref<Shop | null>(null)
const categories = ref<ContentCategory[]>([])
const districts = ref<District[]>([])
const subDistricts = ref<SubDistrict[]>([])

const form = reactive<UserContentFormData & { shopId?: string | null }>({
  title: '',
  summary: '',
  contentCategoryId: null,
  shopId: null,
  districtId: null,
  subDistrictId: null,
  latitude: null,
  longitude: null,
  youtubeUrl: '',
  status: 'Published',
})

const schedulesForm = ref<ScheduleItemForm[]>([])

async function loadSubDistricts(districtId: string | null, keepValue = false) {
  if (!districtId) {
    subDistricts.value = []
    form.subDistrictId = null
    return
  }
  subDistricts.value = await getSubDistricts(districtId)
  if (!keepValue) form.subDistrictId = null
}

watch(
  () => form.districtId,
  (value, oldValue) => {
    if (value && value !== oldValue) void loadSubDistricts(value)
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
    form.districtId = matchedDistrict.districtId
    await loadSubDistricts(matchedDistrict.districtId, true)

    if (data.subDistrictName) {
      const targetSubName = data.subDistrictName.replace(/^(ตำบล|ต\.)\s*/, '').trim()
      const matchedSub = subDistricts.value.find((s) => {
        const sName = s.subDistrictName.replace(/^(ตำบล|ต\.)\s*/, '').trim()
        return sName.includes(targetSubName) || targetSubName.includes(sName)
      })

      if (matchedSub) {
        form.subDistrictId = matchedSub.subDistrictId
      }
    }
  }
}

const selectedCategoryName = computed(
  () => categories.value.find((c) => c.contentCategoryId === form.contentCategoryId)?.categoryName,
)
const selectedCategoryRule = computed(() => getCategoryRule(selectedCategoryName.value))

function addScheduleRow() {
  schedulesForm.value.push({
    title: '',
    startDateTime: '',
    endDateTime: '',
    address: '',
  })
}

function removeScheduleRow(index: number) {
  schedulesForm.value.splice(index, 1)
}

async function load() {
  try {
    const [myShop, catData, distData] = await Promise.all([
      getMyShop(),
      getContentCategories(),
      getDistricts(),
    ])
    shop.value = myShop
    categories.value = catData
    districts.value = distData
    form.shopId = myShop.shopId

    if (isEdit.value) {
      const content = await getOwnedShopContent(contentId.value)
      if (content.status === 'Archived') {
        await router.replace('/my-shop/contents')
        return
      }
      Object.assign(form, {
        title: content.title,
        summary: content.summary ?? '',
        contentCategoryId: content.contentCategoryId,
        shopId: myShop.shopId,
        districtId: content.districtId ?? myShop.districtId ?? null,
        subDistrictId: content.subDistrictId ?? myShop.subDistrictId ?? null,
        latitude: content.latitude ?? myShop.latitude ?? null,
        longitude: content.longitude ?? myShop.longitude ?? null,
        youtubeUrl: content.youtubeUrl ?? '',
        status: (content.status === 'Draft' ? 'Draft' : 'Published') as SubmissionStatus,
      })
      await loadSubDistricts(form.districtId, true)

      // โหลดข้อมูลตารางเวลา/กิจกรรมเดิม
      const existingSchs = await getMyContentSchedules(contentId.value).catch(() => [])
      schedulesForm.value = existingSchs.map((s) => ({
        scheduleId: s.scheduleId,
        title: s.title,
        startDateTime: s.startDateTime ? new Date(s.startDateTime).toISOString().slice(0, 16) : '',
        endDateTime: s.endDateTime ? new Date(s.endDateTime).toISOString().slice(0, 16) : '',
        address: s.address || '',
      }))
    } else {
      form.districtId = myShop.districtId ?? null
      form.subDistrictId = myShop.subDistrictId ?? null
      form.latitude = myShop.latitude ?? null
      form.longitude = myShop.longitude ?? null
      form.status = 'Published'
      if (form.districtId) await loadSubDistricts(form.districtId, true)
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
    if (isEdit.value) await router.replace('/my-shop/contents')
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!form.title.trim() || !form.contentCategoryId) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณาระบุชื่อเรื่องราวและหมวดหมู่')
    return
  }
  saving.value = true
  try {
    const payload = {
      ...form,
      shopId: shop.value?.shopId ?? null,
      latitude: toNullableNumber(form.latitude),
      longitude: toNullableNumber(form.longitude),
      status: form.status || 'Published',
    }
    let targetContentId = contentId.value
    if (isEdit.value) {
      await updateMyContent(contentId.value, payload)
    } else {
      const res = await createShopContent(payload)
      targetContentId = res.contentId
    }

    // บันทึกตารางเวลากิจกรรม
    if (
      targetContentId &&
      selectedCategoryRule.value.hasSchedule !== false &&
      schedulesForm.value.length > 0
    ) {
      for (const item of schedulesForm.value) {
        if (!item.title.trim() || !item.startDateTime) continue
        const schPayload = {
          contentId: targetContentId,
          title: item.title.trim(),
          startDateTime: item.startDateTime,
          endDateTime: item.endDateTime || null,
          address: item.address?.trim() || null,
          latitude: form.latitude,
          longitude: form.longitude,
          description: null,
          status: 'Active' as const,
        }
        if (item.scheduleId) {
          await updateSchedule(item.scheduleId, schPayload).catch(() => undefined)
        } else {
          await createSchedule(schPayload).catch(() => undefined)
        }
      }
    }
    await swal.success(
      isEdit.value ? 'บันทึกการแก้ไขแล้ว' : 'บันทึกเรื่องราวร้านค้าแล้ว',
      form.status === 'Published'
        ? 'คอนเทนต์แสดงผลต่อสาธารณะทันที'
        : 'บันทึกเป็นฉบับร่างเรียบร้อยแล้ว',
    )
    await router.push('/my-shop/contents')
  } catch (error) {
    await swal.error('บันทึกไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 pb-20 text-slate-900">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-4">
      <div class="h-28 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-4">
          <div class="h-64 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
          <div class="h-80 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
        </div>
        <div class="h-96 w-full animate-pulse rounded-2xl bg-white border-2 border-slate-200"></div>
      </div>
    </div>

    <template v-else>
      <!-- 1. Header Bar (High Contrast, Bold, Clear) -->
      <div
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs"
      >
        <div class="space-y-1">
          <RouterLink
            to="/my-shop/contents"
            class="inline-flex items-center gap-1.5 text-xs font-bold text-slate-600 hover:text-emerald-800 transition"
          >
            <i class="mdi mdi-arrow-left text-base" />
            <span>กลับไปรายการคอนเทนต์</span>
          </RouterLink>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl sm:text-3xl font-black text-slate-950 tracking-tight">
              {{ isEdit ? 'แก้ไขเรื่องราวร้านค้า' : 'เขียนเรื่องราวร้านค้าใหม่' }}
            </h1>
            <span
              class="inline-flex items-center gap-1.5 rounded-full px-4 py-1 text-xs font-bold shadow-xs border"
              :class="
                form.status === 'Published'
                  ? 'bg-emerald-100 text-emerald-950 border-emerald-300'
                  : 'bg-amber-100 text-amber-950 border-amber-300'
              "
            >
              <span
                class="h-2 w-2 rounded-full"
                :class="form.status === 'Published' ? 'bg-emerald-600' : 'bg-amber-500'"
              ></span>
              <span>{{ form.status === 'Published' ? 'เผยแพร่ทันที' : 'บันทึกร่าง' }}</span>
            </span>
          </div>
          <p class="text-xs sm:text-sm text-slate-700 font-bold">
            สร้างคอนเทนต์ บทความ วิดีโอแนะนำสินค้า และจุดเด่นของร้านค้า
          </p>
        </div>

        <!-- Header Actions: Status Pill + Save Button -->
        <div class="flex items-center gap-3 self-start sm:self-auto flex-wrap">
          <!-- Status Selector Pill (Capsule Design - No Squares) -->
          <div class="inline-flex items-center rounded-full bg-slate-100 p-1.5 border-2 border-slate-300 shadow-inner">
            <button
              type="button"
              class="flex items-center gap-2 rounded-full px-4 py-2 text-xs font-black transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
              :class="
                form.status === 'Published'
                  ? 'bg-emerald-600 text-white shadow-sm'
                  : 'text-slate-700 hover:text-slate-950 bg-transparent'
              "
              @click="form.status = 'Published'"
            >
              <span
                class="h-2 w-2 rounded-full"
                :class="form.status === 'Published' ? 'bg-white shadow-2xs' : 'bg-emerald-600'"
              ></span>
              <span>เผยแพร่ทันที</span>
            </button>
            <button
              type="button"
              class="flex items-center gap-2 rounded-full px-4 py-2 text-xs font-black transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
              :class="
                form.status === 'Draft'
                  ? 'bg-amber-500 text-white shadow-sm'
                  : 'text-slate-700 hover:text-slate-950 bg-transparent'
              "
              @click="form.status = 'Draft'"
            >
              <span
                class="h-2 w-2 rounded-full"
                :class="form.status === 'Draft' ? 'bg-white shadow-2xs' : 'bg-amber-500'"
              ></span>
              <span>บันทึกร่าง</span>
            </button>
          </div>

          <!-- Save Button (Capsule Style) -->
          <button
            type="button"
            class="inline-flex items-center gap-2 rounded-full bg-emerald-700 hover:bg-emerald-800 px-6 py-2.5 font-black text-xs sm:text-sm text-white shadow-sm transition active:scale-95 border border-emerald-700 cursor-pointer select-none"
            :disabled="saving"
            @click="save"
          >
            <i
              class="mdi text-base text-white"
              :class="saving ? 'mdi-loading animate-spin' : 'mdi-content-save-check'"
            />
            <span>{{ saving ? 'กำลังบันทึก...' : isEdit ? 'บันทึกการแก้ไข' : 'บันทึกเรื่องราว' }}</span>
          </button>
        </div>
      </div>

      <!-- 2. Main Form Grid (2 Columns: Form Fields + Preview / Guide) -->
      <form class="grid gap-6 lg:grid-cols-12 items-start" @submit.prevent="save">
        <!-- Left Column: Form Details (8 Columns) -->
        <div class="lg:col-span-8 space-y-6">
          <!-- Section 1: Story Details & YouTube Video -->
          <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-5">
            <div class="flex items-center gap-2.5 border-b-2 border-slate-100 pb-3.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-100 text-emerald-800 font-bold">
                <i class="mdi mdi-file-document-edit text-xl text-emerald-800"></i>
              </span>
              <div>
                <h2 class="text-base font-black text-slate-950">เนื้อหาและวิดีโอแนะนำ</h2>
                <p class="text-xs font-bold text-slate-600">ระบุชื่อเรื่อง บทคัดย่อ และลิงก์วิดีโอ</p>
              </div>
            </div>

            <div class="space-y-4">
              <AppTextField
                v-model="form.title"
                label="ชื่อเรื่องราว / คอนเทนต์ *"
                placeholder="เช่น ประวัติความเป็นมาและสูตรขนมทองพับโบราณเมืองกาญจน์"
              />

              <AppTextarea
                v-model="form.summary"
                label="คำอธิบายย่อ (สรุปเนื้อหาบทความ)"
                placeholder="เล่าเรื่องราว จุดเด่น กรรมวิธีการผลิต หรือความประทับใจที่เป็นประโยชน์แก่ผู้เข้าชม..."
                rows="4"
              />

              <div class="space-y-2">
                <AppTextField
                  v-model="form.youtubeUrl"
                  label="URL วิดีโอ YouTube (ถ้ามี)"
                  placeholder="https://www.youtube.com/watch?v=..."
                />

                <!-- Video Preview -->
                <div
                  v-if="youtubeEmbedUrl(form.youtubeUrl)"
                  class="overflow-hidden rounded-2xl border-2 border-slate-300 bg-slate-900 shadow-xs mt-2"
                >
                  <iframe
                    :src="youtubeEmbedUrl(form.youtubeUrl)"
                    title="ตัวอย่างวิดีโอ YouTube"
                    class="aspect-video w-full"
                    allowfullscreen
                  />
                </div>
                <p v-else-if="form.youtubeUrl" class="text-xs text-rose-700 font-bold">
                  กรุณาใส่ลิงก์ YouTube ที่ถูกต้อง เช่น https://www.youtube.com/watch?v=...
                </p>
              </div>
            </div>
          </section>

          <!-- Section 2: Category and Location -->
          <section class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-5">
            <div class="flex items-center gap-2.5 border-b-2 border-slate-100 pb-3.5">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-teal-100 text-teal-800 font-bold">
                <i class="mdi mdi-map-marker-radius text-xl text-teal-800"></i>
              </span>
              <div>
                <h2 class="text-base font-black text-slate-950">หมวดหมู่และพิกัดที่ตั้ง</h2>
                <p class="text-xs font-bold text-slate-600">เลือกหมวดหมู่และปักหมุดตำแหน่งบนแผนที่</p>
              </div>
            </div>

            <div class="grid gap-4 sm:grid-cols-2">
              <AppSelect
                v-model="form.contentCategoryId"
                label="หมวดหมู่บทความ *"
                :items="categories"
                item-title="categoryName"
                item-value="contentCategoryId"
                placeholder="เลือกหมวดหมู่"
              />
              <AppSelect
                v-model="form.districtId"
                label="อำเภอ (เลือกอัตโนมัติเมื่อปักหมุด GPS)"
                :items="districts"
                item-title="districtName"
                item-value="districtId"
                placeholder="เลือกอำเภอ"
                clearable
              />
              <AppSelect
                v-model="form.subDistrictId"
                label="ตำบล (เลือกอัตโนมัติเมื่อปักหมุด GPS)"
                :items="subDistricts"
                item-title="subDistrictName"
                item-value="subDistrictId"
                placeholder="เลือกตำบล"
                :disabled="!form.districtId"
                clearable
              />
            </div>

            <!-- Category Rule Notice Box -->
            <div
              v-if="selectedCategoryName"
              class="p-4 rounded-xl bg-slate-50 border-2 border-slate-200 text-xs text-slate-900 space-y-1.5"
            >
              <div class="flex items-center gap-1.5 text-emerald-800 font-black">
                <i class="mdi mdi-information text-base"></i>
                <span>เงื่อนไขหมวดหมู่: {{ selectedCategoryName }}</span>
              </div>
              <p class="font-bold text-slate-700">
                • เชื่อมโยงกับร้านค้า:
                <span class="font-black text-slate-900">{{ shop?.shopName }}</span>
                โดยอัตโนมัติ
              </p>
              <p class="font-bold text-slate-700">
                • ส่วนตารางเวลากิจกรรม:
                <span class="font-black text-slate-900">{{
                  selectedCategoryRule.hasSchedule === false
                    ? 'ซ่อนตารางเวลาสำหรับหมวดหมู่นี้'
                    : 'เปิดให้เพิ่มตารางเวลากิจกรรมได้'
                }}</span>
              </p>
            </div>

            <!-- Map Location Picker -->
            <div class="pt-2">
              <label class="block text-xs sm:text-sm font-bold text-slate-900 mb-2">
                ปักหมุดพิกัดสถานที่บนแผนที่
              </label>
              <LocationPickerMap
                v-model:latitude="form.latitude"
                v-model:longitude="form.longitude"
                @address-detected="onAddressDetected"
              />
            </div>
          </section>

          <!-- Section 3: Schedule & Events (if applicable) -->
          <section
            v-if="selectedCategoryRule.hasSchedule !== false"
            class="rounded-2xl border-2 border-slate-200 bg-white p-6 shadow-xs space-y-5"
          >
            <div class="flex items-center justify-between border-b-2 border-slate-100 pb-3.5">
              <div class="flex items-center gap-2.5">
                <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-purple-100 text-purple-800 font-bold">
                  <i class="mdi mdi-calendar-clock text-xl text-purple-800"></i>
                </span>
                <div>
                  <h2 class="text-base font-black text-slate-950">ตารางเวลาและรอบกิจกรรม</h2>
                  <p class="text-xs font-bold text-slate-600">กำหนดรอบเวลาจัดกิจกรรมหรือการแสดง</p>
                </div>
              </div>

              <button
                type="button"
                class="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl bg-emerald-700 hover:bg-emerald-800 text-white text-xs font-bold transition cursor-pointer select-none"
                @click="addScheduleRow"
              >
                <i class="mdi mdi-plus text-sm"></i>
                <span>เพิ่มตารางเวลา</span>
              </button>
            </div>

            <div
              v-if="schedulesForm.length === 0"
              class="py-8 text-center text-xs font-bold text-slate-500 border-2 border-dashed border-slate-200 rounded-2xl bg-slate-50"
            >
              ยังไม่มีตารางเวลา กดปุ่ม "+ เพิ่มตารางเวลา" เพื่อระบุรอบวันเวลาและสถานที่
            </div>

            <div v-else class="space-y-3.5">
              <div
                v-for="(item, idx) in schedulesForm"
                :key="idx"
                class="p-4 rounded-xl border-2 border-slate-200 bg-slate-50 space-y-3"
              >
                <div class="flex items-center justify-between">
                  <span class="text-xs font-black text-emerald-900 bg-emerald-100 border border-emerald-300 px-2 py-0.5 rounded-md">
                    รอบกิจกรรมที่ {{ idx + 1 }}
                  </span>
                  <button
                    type="button"
                    class="text-rose-700 hover:text-rose-900 text-xs font-bold flex items-center gap-1 cursor-pointer"
                    @click="removeScheduleRow(idx)"
                  >
                    <i class="mdi mdi-trash-can-outline"></i>
                    <span>ลบรายการนี้</span>
                  </button>
                </div>
                <div class="grid gap-3 sm:grid-cols-2">
                  <AppTextField
                    v-model="item.title"
                    label="หัวข้อกิจกรรม / ตาราง *"
                    placeholder="เช่น พิธีตักบาตรมอญ หรือ รอบการแสดง"
                  />
                  <AppTextField
                    v-model="item.address"
                    label="สถานที่ / จุดจัดงาน"
                    placeholder="เช่น สะพานมอญ หรือ เวทีกลาง"
                  />
                  <AppTextField
                    v-model="item.startDateTime"
                    type="datetime-local"
                    label="วัน-เวลาเริ่มต้น *"
                  />
                  <AppTextField
                    v-model="item.endDateTime"
                    type="datetime-local"
                    label="วัน-เวลาสิ้นสุด (ถ้ามี)"
                  />
                </div>
              </div>
            </div>
          </section>
        </div>

        <!-- Right Column: Live Preview & Guide (4 Columns) -->
        <aside class="lg:col-span-4 space-y-5 lg:sticky lg:top-6">
          <!-- Live Preview Card (WOW factor) -->
          <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 shadow-xs space-y-3.5">
            <div class="flex items-center justify-between border-b-2 border-slate-100 pb-2.5">
              <span class="text-xs font-black uppercase tracking-wider text-slate-800 flex items-center gap-1.5">
                <i class="mdi mdi-eye-outline text-emerald-700 text-base"></i>
                ตัวอย่างการแสดงผลบนเว็บ
              </span>
              <span class="text-[11px] font-bold text-slate-500">Live Preview</span>
            </div>

            <!-- Preview Card Item -->
            <div class="overflow-hidden rounded-xl border-2 border-slate-200 bg-white shadow-2xs">
              <div class="relative aspect-16/10 w-full overflow-hidden bg-slate-900">
                <img
                  v-if="youtubeThumbnail(form.youtubeUrl)"
                  :src="youtubeThumbnail(form.youtubeUrl)"
                  alt="ภาพจำลอง"
                  class="h-full w-full object-cover"
                />
                <div
                  v-else
                  class="flex h-full w-full items-center justify-center bg-gradient-to-br from-emerald-800 to-slate-900 text-white"
                >
                  <i class="mdi mdi-text-box-outline text-4xl opacity-40"></i>
                </div>

                <div class="absolute top-2.5 left-2.5">
                  <span class="rounded-md bg-slate-900/90 text-white px-2 py-0.5 text-[10px] font-black">
                    {{ selectedCategoryName || 'หมวดหมู่' }}
                  </span>
                </div>
              </div>

              <div class="p-3.5 space-y-1.5">
                <h3 class="text-sm font-black text-slate-950 line-clamp-2 leading-snug">
                  {{ form.title || 'ระบุชื่อเรื่องราวของคุณ' }}
                </h3>
                <p class="text-xs font-medium text-slate-600 line-clamp-2 leading-relaxed">
                  {{ form.summary || 'คำอธิบายสรุปจะแสดงตรงนี้...' }}
                </p>
                <div class="text-[11px] font-bold text-emerald-800 pt-1 flex items-center gap-1">
                  <i class="mdi mdi-storefront text-xs"></i>
                  <span>{{ shop?.shopName || 'ร้านค้าของคุณ' }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- Shop Connection Notice -->
          <div class="rounded-2xl border-2 border-emerald-200 bg-emerald-50/60 p-5 space-y-2">
            <div class="flex items-center gap-2 text-emerald-900 font-black text-sm">
              <i class="mdi mdi-storefront text-lg text-emerald-700"></i>
              <span>เชื่อมโยงร้านค้าอัตโนมัติ</span>
            </div>
            <p class="text-xs font-bold text-slate-700 leading-relaxed">
              เรื่องราวนี้จะถูกผูกกับร้าน
              <strong class="text-slate-950 font-black">{{ shop?.shopName }}</strong>
              และแสดงผลให้ผู้เข้าชมบนหน้าร้านและหน้าสำรวจทันที
            </p>
          </div>

          <!-- Tips Guide -->
          <div class="rounded-2xl border-2 border-slate-200 bg-white p-5 space-y-2.5 text-xs text-slate-700 font-bold">
            <h3 class="text-sm font-black text-slate-950 flex items-center gap-1.5">
              <i class="mdi mdi-lightbulb-on-outline text-amber-500 text-base"></i>
              <span>คำแนะนำในการเขียน</span>
            </h3>
            <ul class="space-y-1.5 pt-1">
              <li>• เลือก <strong>"เผยแพร่ทันที"</strong> เพื่อให้บทความแสดงผลสาธารณะทันที</li>
              <li>• แนบลิงก์ <strong>YouTube</strong> เพื่อให้บทความมีวิดีโอเล่นได้โดยตรง</li>
              <li>• กดปุ่ม <strong>"ใช้ตำแหน่งปัจจุบัน"</strong> บนแผนที่เพื่อเลือกพิกัดและอำเภอให้อัตโนมัติ</li>
            </ul>
          </div>
        </aside>
      </form>

      <!-- 3. Sticky Bottom Action Bar -->
      <div
        class="sticky bottom-4 z-20 flex items-center justify-between gap-3 rounded-2xl border-2 border-slate-200 bg-white/95 p-4 shadow-lg backdrop-blur-md"
      >
        <RouterLink
          to="/my-shop/contents"
          class="rounded-xl border-2 border-slate-300 bg-white px-5 py-2.5 text-xs sm:text-sm font-bold text-slate-800 hover:bg-slate-50 transition"
        >
          ยกเลิก
        </RouterLink>

        <button
          type="button"
          class="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-700 hover:bg-emerald-800 px-6 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 disabled:opacity-50 cursor-pointer border border-emerald-700"
          :disabled="saving"
          @click="save"
        >
          <i
            class="mdi text-base text-white"
            :class="saving ? 'mdi-loading animate-spin' : 'mdi-content-save-check'"
          />
          <span class="text-white">{{
            saving ? 'กำลังบันทึกข้อมูล...' : isEdit ? 'บันทึกการแก้ไขเรื่องราว' : 'บันทึกเรื่องราวใหม่'
          }}</span>
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
button,
a,
input,
select,
textarea {
  outline: none !important;
  -webkit-tap-highlight-color: transparent !important;
}

button:focus,
button:focus-visible,
button:active {
  outline: none !important;
  box-shadow: none !important;
}
</style>
