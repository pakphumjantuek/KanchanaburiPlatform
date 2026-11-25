<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  createMyContent,
  getContentCategories,
  getDistricts,
  getMyContent,
  getShops,
  getSubDistricts,
  updateMyContent,
  type ContentCategory,
  type District,
  type SubmissionStatus,
  type SubDistrict,
  type UserContentFormData,
} from '@/features/contents/api'
import AppSelect from '@/components/common/input/AppSelect.vue'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'
import LocationPickerMap from '@/components/common/map/LocationPickerMap.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const route = useRoute()
const router = useRouter()
const swal = useSwal()
const contentId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => Boolean(contentId.value))
const loading = ref(true)
const saving = ref(false)
const categories = ref<ContentCategory[]>([])
const districts = ref<District[]>([])
const subDistricts = ref<SubDistrict[]>([])
const shops = ref<{ shopId: string; shopName: string }[]>([])


const form = reactive<UserContentFormData>({
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

function youtubeEmbedUrl(url: string) {
  try {
    const parsed = new URL(url)
    const videoId = parsed.hostname.includes('youtu.be')
      ? parsed.pathname.slice(1)
      : (parsed.searchParams.get('v') ??
        (parsed.pathname.startsWith('/embed/') ? parsed.pathname.split('/')[2] : ''))
    return videoId ? `https://www.youtube-nocookie.com/embed/${videoId}` : ''
  } catch {
    return ''
  }
}

function toNullableNumber(value: number | string | null) {
  if (value === null || value === '') return null
  const number = Number(value)
  return Number.isFinite(number) ? number : null
}

async function load() {
  try {
    ;[categories.value, districts.value, shops.value] = await Promise.all([
      getContentCategories(),
      getDistricts(),
      getShops().catch(() => []),
    ])
    if (isEdit.value) {
      const content = await getMyContent(contentId.value)
      if (content.status === 'Archived') {
        await router.replace('/profile')
        return
      }
      Object.assign(form, {
        title: content.title,
        summary: content.summary ?? '',
        contentCategoryId: content.contentCategoryId,
        shopId: content.shopId ?? null,
        districtId: content.districtId ?? null,
        subDistrictId: content.subDistrictId ?? null,
        latitude: content.latitude ?? null,
        longitude: content.longitude ?? null,
        youtubeUrl: content.youtubeUrl ?? '',
        status: (content.status === 'Draft' ? 'Draft' : 'Published') as SubmissionStatus,
      })
      await loadSubDistricts(form.districtId, true)

      // โหลดข้อมูลตารางเวลา/กิจกรรมที่มีอยู่เดิม
      const existingSchs = await getMyContentSchedules(contentId.value).catch(() => [])
      schedulesForm.value = existingSchs.map((s) => ({
        scheduleId: s.scheduleId,
        title: s.title,
        startDateTime: s.startDateTime ? new Date(s.startDateTime).toISOString().slice(0, 16) : '',
        endDateTime: s.endDateTime ? new Date(s.endDateTime).toISOString().slice(0, 16) : '',
        address: s.address || '',
      }))
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
    if (isEdit.value) await router.replace('/profile')
  } finally {
    loading.value = false
  }
}

import { getCategoryRule } from '@/features/contents/constants/categoryRules'
import { getMyContentSchedules } from '@/features/contents/api'
import {
  archiveSchedule,
  createSchedule,
  updateSchedule,
} from '@/features/admin/schedules/api/adminScheduleApi'

const selectedCategoryName = computed(
  () => categories.value.find((c) => c.contentCategoryId === form.contentCategoryId)?.categoryName,
)
const selectedCategoryRule = computed(() => getCategoryRule(selectedCategoryName.value))

interface ScheduleItemForm {
  scheduleId?: string
  title: string
  startDateTime: string
  endDateTime: string
  address: string
}

const schedulesForm = ref<ScheduleItemForm[]>([])
const removedScheduleIds = ref<string[]>([])

function addScheduleRow() {
  schedulesForm.value.push({
    title: '',
    startDateTime: '',
    endDateTime: '',
    address: '',
  })
}

function removeScheduleRow(index: number) {
  const schedule = schedulesForm.value[index]

  if (schedule?.scheduleId) {
    removedScheduleIds.value.push(schedule.scheduleId)
  }

  schedulesForm.value.splice(index, 1)
}

// Show what is still missing only after the user has tried to save
const attempted = ref(false)
const missingFields = computed(() => {
  const list: string[] = []
  if (!form.title.trim()) list.push('ชื่อคอนเทนต์')
  if (!form.contentCategoryId) list.push('หมวดหมู่')
  return list
})
const progressItems = computed(() => [
  { label: 'ชื่อคอนเทนต์', done: Boolean(form.title.trim()), required: true },
  { label: 'หมวดหมู่', done: Boolean(form.contentCategoryId), required: true },
  { label: 'สรุปเนื้อหาย่อ', done: Boolean(form.summary?.trim()), required: false },
  { label: 'ปักหมุดตำแหน่ง', done: form.latitude !== null && form.longitude !== null, required: false },
  { label: 'วิดีโอ YouTube', done: Boolean(youtubeEmbedUrl(form.youtubeUrl)), required: false },
])

async function save() {
  attempted.value = true
  if (!form.title.trim() || !form.contentCategoryId) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณาระบุชื่อคอนเทนต์และหมวดหมู่')
    return
  }
  saving.value = true
  try {
    const payload = {
      ...form,
      latitude: toNullableNumber(form.latitude),
      longitude: toNullableNumber(form.longitude),
      status: form.status || 'Published',
    }
    let targetContentId = contentId.value
    if (isEdit.value) {
      await updateMyContent(contentId.value, payload)
    } else {
      const res = await createMyContent(payload)
      targetContentId = res.contentId
    }

    // ลบรายการเดิมที่ผู้ใช้เอาออกจากฟอร์ม
    for (const scheduleId of removedScheduleIds.value) {
      await archiveSchedule(scheduleId)
    }

    // บันทึกตารางเวลา/กิจกรรมที่ยังเหลืออยู่ หรือเพิ่มรายการใหม่
    if (targetContentId && selectedCategoryRule.value.hasSchedule !== false) {
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
          await updateSchedule(item.scheduleId, schPayload)
        } else {
          await createSchedule(schPayload)
        }
      }
    }

    await swal.success(
      isEdit.value ? 'บันทึกการแก้ไขแล้ว' : 'บันทึกคอนเทนต์แล้ว',
      form.status === 'Published'
        ? 'คอนเทนต์ของคุณแสดงผลต่อสาธารณะทันที'
        : 'บันทึกเป็นฉบับร่างเรียบร้อยแล้ว',
    )
    await router.push('/profile')
  } catch (error) {
    await swal.error('บันทึกไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="min-h-screen bg-[#F7F0E6] pb-10 font-sans text-[#332820]">
    <main class="mx-auto w-full max-w-6xl px-4 py-7 sm:px-6 lg:py-10">
      <!-- Skeleton Loading -->
      <div v-if="loading" class="space-y-6">
        <div class="h-24 w-full animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2]"></div>
        <div class="h-96 w-full animate-pulse rounded-3xl border border-[#E8D9C9] bg-[#FFF9F2]"></div>
      </div>

      <template v-else>
        <!-- Header -->
        <div class="mb-6">
          <RouterLink
            to="/profile"
            class="inline-flex items-center gap-1 text-sm font-black text-[#D96C2C] hover:underline"
          >
            <i class="mdi mdi-arrow-left" /> กลับไปหน้าโปรไฟล์
          </RouterLink>
          <h1 class="mt-2 text-2xl leading-[1.35] font-black text-[#332820] sm:text-3xl">
            {{ isEdit ? 'แก้ไขคอนเทนต์' : 'สร้างคอนเทนต์ใหม่' }}
          </h1>
          <p class="mt-1 text-sm font-semibold text-[#786B62]">
            บอกเล่าเรื่องราว สถานที่ท่องเที่ยว วัฒนธรรม หรือประสบการณ์ในกาญจนบุรี
            กรอกเพียง <span class="font-black text-[#D96C2C]">ชื่อ</span> และ
            <span class="font-black text-[#D96C2C]">หมวดหมู่</span> ก็บันทึกได้แล้ว
            ส่วนที่เหลือเติมเมื่อพร้อม
          </p>
        </div>

        <!-- Missing-fields banner (only after a failed save attempt) -->
        <div
          v-if="attempted && missingFields.length"
          class="mb-6 flex items-start gap-3 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-800"
          role="alert"
        >
          <i class="mdi mdi-alert-circle-outline text-xl"></i>
          <p>ยังขาดข้อมูลที่จำเป็น: {{ missingFields.join(', ') }}</p>
        </div>

        <form
          class="grid items-start gap-6 lg:grid-cols-[minmax(0,1fr)_320px]"
          @submit.prevent="save"
        >
          <div class="space-y-6">
            <!-- Step 1: Details -->
            <section class="space-y-5 rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
              <div class="flex items-start gap-3">
                <span
                  class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-sm font-black text-white"
                  >1</span
                >
                <div>
                  <h2 class="text-lg font-black text-[#332820]">เล่าเรื่องของคุณ</h2>
                  <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
                    ตั้งชื่อให้น่าสนใจและสรุปสั้นๆ ว่าเรื่องนี้เกี่ยวกับอะไร
                  </p>
                </div>
              </div>

              <AppTextField
                v-model="form.title"
                label="ชื่อคอนเทนต์ * (จำเป็น)"
                placeholder="เช่น มนต์เสน่ห์สังขละบุรีและวิถีชีวิตชาวมอญ"
              />
              <p
                v-if="attempted && !form.title.trim()"
                class="-mt-3 text-xs font-bold text-rose-600"
              >
                กรุณาใส่ชื่อคอนเทนต์
              </p>

              <AppTextarea
                v-model="form.summary"
                label="สรุปเนื้อหาย่อ (ไม่บังคับ)"
                placeholder="เขียนสรุปสั้น ๆ 1-2 ประโยค เพื่อดึงดูดผู้อ่าน"
              />

              <AppTextField
                v-model="form.youtubeUrl"
                label="ลิงก์วิดีโอ YouTube (ไม่บังคับ)"
                placeholder="วางลิงก์ เช่น https://www.youtube.com/watch?v=..."
              />
              <div
                v-if="youtubeEmbedUrl(form.youtubeUrl)"
                class="overflow-hidden rounded-2xl border border-[#E8D9C9] bg-[#171412]"
              >
                <iframe
                  :src="youtubeEmbedUrl(form.youtubeUrl)"
                  title="ตัวอย่างวิดีโอ YouTube"
                  class="aspect-video w-full"
                  allowfullscreen
                />
              </div>
              <p v-else-if="form.youtubeUrl" class="text-xs font-bold text-rose-600">
                ลิงก์นี้ใช้ไม่ได้ กรุณาวางลิงก์ YouTube ที่ถูกต้อง
              </p>
            </section>

            <!-- Step 2: Category, Shop & Location -->
            <section class="space-y-5 rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6">
              <div class="flex items-start gap-3">
                <span
                  class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-sm font-black text-white"
                  >2</span
                >
                <div>
                  <h2 class="text-lg font-black text-[#332820]">หมวดหมู่และสถานที่</h2>
                  <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
                    เลือกหมวดหมู่ แล้วคลิกบนแผนที่เพื่อปักหมุด อำเภอและตำบลจะถูกเลือกให้อัตโนมัติ
                  </p>
                </div>
              </div>

              <div class="grid gap-5 sm:grid-cols-2">
                <AppSelect
                  v-model="form.contentCategoryId"
                  label="หมวดหมู่ * (จำเป็น)"
                  :items="categories"
                  item-title="categoryName"
                  item-value="contentCategoryId"
                  placeholder="เลือกหมวดหมู่"
                />

                <AppSelect
                  v-model="form.shopId"
                  label="ร้านที่เกี่ยวข้อง (ไม่บังคับ)"
                  :items="shops"
                  item-title="shopName"
                  item-value="shopId"
                  placeholder="เลือกร้านที่คุณกล่าวถึง หรือเว้นว่างได้"
                  clearable
                />
              </div>
              <p
                v-if="attempted && !form.contentCategoryId"
                class="-mt-3 text-xs font-bold text-rose-600"
              >
                กรุณาเลือกหมวดหมู่
              </p>

              <div class="grid gap-5 sm:grid-cols-2">
                <AppSelect
                  v-model="form.districtId"
                  label="อำเภอ"
                  :items="districts"
                  item-title="districtName"
                  item-value="districtId"
                  placeholder="เลือกอำเภอ หรือปักหมุดเพื่อเลือกอัตโนมัติ"
                  clearable
                />
                <AppSelect
                  v-model="form.subDistrictId"
                  label="ตำบล"
                  :items="subDistricts"
                  item-title="subDistrictName"
                  item-value="subDistrictId"
                  :placeholder="form.districtId ? 'เลือกตำบล' : 'เลือกอำเภอก่อน'"
                  :disabled="!form.districtId"
                  clearable
                />
              </div>

              <LocationPickerMap
                v-model:latitude="form.latitude"
                v-model:longitude="form.longitude"
                @address-detected="onAddressDetected"
              />
            </section>

            <!-- Step 3: Schedule & Events (if applicable) -->
            <section
              v-if="selectedCategoryRule.hasSchedule !== false"
              class="space-y-5 rounded-3xl border border-[#E8D9C9] bg-white p-5 sm:p-6"
            >
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div class="flex items-start gap-3">
                  <span
                    class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-sm font-black text-white"
                    >3</span
                  >
                  <div>
                    <h2 class="text-lg font-black text-[#332820]">
                      วันเวลาและกิจกรรม
                      <span class="ml-1 text-xs font-bold text-[#786B62]">(ไม่บังคับ)</span>
                    </h2>
                    <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
                      ถ้ามีงานหรือรอบกิจกรรม ระบุวัน เวลา และสถานที่ได้ เพิ่มได้หลายรายการ
                    </p>
                  </div>
                </div>
                <button
                  type="button"
                  class="inline-flex cursor-pointer items-center gap-1.5 rounded-full bg-[#FFF1E8] px-4 py-2 text-xs font-black text-[#BF5720] transition hover:bg-[#D96C2C] hover:text-white"
                  @click="addScheduleRow"
                >
                  <i class="mdi mdi-plus text-sm"></i>
                  เพิ่มวันเวลา
                </button>
              </div>

              <div
                v-if="schedulesForm.length === 0"
                class="rounded-2xl border-2 border-dashed border-[#E8D9C9] py-7 text-center text-sm font-semibold text-[#786B62]"
              >
                ยังไม่มีวันเวลา — ข้ามได้เลย หรือกด “เพิ่มวันเวลา” ถ้ามีงาน/กิจกรรม
              </div>

              <div v-else class="space-y-4">
                <div
                  v-for="(item, idx) in schedulesForm"
                  :key="idx"
                  class="space-y-3 rounded-2xl border border-[#E8D9C9] bg-[#FFF9F2] p-4"
                >
                  <div class="flex items-center justify-between">
                    <span class="text-xs font-black text-[#D96C2C]">รายการที่ {{ idx + 1 }}</span>
                    <button
                      type="button"
                      class="flex cursor-pointer items-center gap-1 text-xs font-bold text-rose-600 hover:text-rose-800"
                      @click="removeScheduleRow(idx)"
                    >
                      <i class="mdi mdi-trash-can-outline"></i> ลบรายการนี้
                    </button>
                  </div>
                  <div class="grid gap-3 sm:grid-cols-2">
                    <AppTextField
                      v-model="item.title"
                      label="ชื่อกิจกรรม *"
                      placeholder="เช่น พิธีตักบาตรมอญ หรือ รอบการแสดง"
                    />
                    <AppTextField
                      v-model="item.address"
                      label="สถานที่จัดงาน"
                      placeholder="เช่น สะพานมอญ หรือ เวทีกลาง"
                    />
                    <AppTextField
                      v-model="item.startDateTime"
                      type="datetime-local"
                      label="เริ่มวันที่-เวลา *"
                    />
                    <AppTextField
                      v-model="item.endDateTime"
                      type="datetime-local"
                      label="สิ้นสุดวันที่-เวลา (ถ้ามี)"
                    />
                  </div>
                </div>
              </div>
            </section>
          </div>

          <!-- Sidebar: progress checklist + tip -->
          <aside class="space-y-5 lg:sticky lg:top-28">
            <section class="rounded-3xl border border-[#E8D9C9] bg-white p-5">
              <h2 class="flex items-center gap-2 text-sm font-black text-[#332820]">
                <i class="mdi mdi-clipboard-check-outline text-lg text-[#D96C2C]"></i>
                เช็กลิสต์ก่อนบันทึก
              </h2>
              <ul class="mt-3 space-y-2">
                <li
                  v-for="p in progressItems"
                  :key="p.label"
                  class="flex items-center gap-2 text-sm font-semibold"
                  :class="p.done ? 'text-[#332820]' : 'text-[#786B62]'"
                >
                  <i
                    class="mdi text-lg"
                    :class="
                      p.done
                        ? 'mdi-check-circle text-emerald-600'
                        : p.required
                          ? 'mdi-alert-circle-outline text-rose-500'
                          : 'mdi-circle-outline text-[#C9B8A8]'
                    "
                  ></i>
                  <span>{{ p.label }}</span>
                  <span
                    v-if="p.required && !p.done"
                    class="ml-auto rounded-full bg-rose-50 px-2 py-0.5 text-[10px] font-black text-rose-600"
                    >จำเป็น</span
                  >
                  <span v-else-if="!p.required && !p.done" class="ml-auto text-[10px] font-bold"
                    >ไม่บังคับ</span
                  >
                </li>
              </ul>
            </section>

            <section class="rounded-3xl border border-[#D96C2C]/25 bg-[#FFF1E8] p-5 text-sm">
              <div class="flex gap-3">
                <i class="mdi mdi-lightbulb-on-outline shrink-0 text-xl text-[#D96C2C]" />
                <div>
                  <h2 class="font-black text-[#BF5720]">เชื่อมเรื่องราวกับร้านค้า</h2>
                  <p class="mt-1 text-xs leading-relaxed font-semibold text-[#786B62] sm:text-sm">
                    เลือกร้านที่คุณไปรีวิวหรือกล่าวถึงได้ บทความยังเป็นของคุณ
                    ร้านไม่สามารถแก้ไขหรือลบบทความและกิจกรรมของคุณได้
                  </p>
                </div>
              </div>
            </section>
          </aside>
        </form>

        <!-- Sticky action bar: visibility choice + save -->
        <div
          class="sticky bottom-3 z-30 mt-6 rounded-3xl border border-[#E8D9C9] bg-white/95 p-3 shadow-xl shadow-[#332820]/10 backdrop-blur-xl sm:p-4"
        >
          <div class="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
            <div class="grid gap-2 sm:grid-cols-2 lg:flex-1" role="radiogroup" aria-label="สถานะการแสดงผล">
              <button
                type="button"
                role="radio"
                :aria-checked="form.status === 'Published'"
                class="flex cursor-pointer items-center gap-3 rounded-2xl border-2 px-4 py-2.5 text-left transition focus-visible:ring-4 focus-visible:ring-emerald-200 focus-visible:outline-none"
                :class="
                  form.status === 'Published'
                    ? 'border-emerald-500 bg-emerald-50'
                    : 'border-[#E8D9C9] bg-white hover:border-emerald-300'
                "
                @click="form.status = 'Published'"
              >
                <i
                  class="mdi text-2xl"
                  :class="
                    form.status === 'Published'
                      ? 'mdi-radiobox-marked text-emerald-600'
                      : 'mdi-radiobox-blank text-[#C9B8A8]'
                  "
                ></i>
                <span>
                  <span class="block text-sm font-black text-[#332820]">เผยแพร่ทันที</span>
                  <span class="block text-xs font-semibold text-[#786B62]">ทุกคนเห็นเรื่องราวนี้</span>
                </span>
              </button>
              <button
                type="button"
                role="radio"
                :aria-checked="form.status === 'Draft'"
                class="flex cursor-pointer items-center gap-3 rounded-2xl border-2 px-4 py-2.5 text-left transition focus-visible:ring-4 focus-visible:ring-amber-200 focus-visible:outline-none"
                :class="
                  form.status === 'Draft'
                    ? 'border-amber-500 bg-amber-50'
                    : 'border-[#E8D9C9] bg-white hover:border-amber-300'
                "
                @click="form.status = 'Draft'"
              >
                <i
                  class="mdi text-2xl"
                  :class="
                    form.status === 'Draft'
                      ? 'mdi-radiobox-marked text-amber-600'
                      : 'mdi-radiobox-blank text-[#C9B8A8]'
                  "
                ></i>
                <span>
                  <span class="block text-sm font-black text-[#332820]">บันทึกเป็นฉบับร่าง</span>
                  <span class="block text-xs font-semibold text-[#786B62]">มีแค่คุณที่เห็น แก้ไขต่อได้</span>
                </span>
              </button>
            </div>

            <button
              type="button"
              class="flex shrink-0 cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] px-8 py-3 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
              :disabled="saving"
              @click="save"
            >
              <i
                class="mdi text-lg text-white"
                :class="saving ? 'mdi-loading animate-spin' : 'mdi-check-circle-outline'"
              />
              <span class="text-white">{{
                saving ? 'กำลังบันทึก...' : form.status === 'Draft' ? 'บันทึกฉบับร่าง' : 'เผยแพร่เรื่องราว'
              }}</span>
            </button>
          </div>
        </div>
      </template>
    </main>
  </div>
</template>
