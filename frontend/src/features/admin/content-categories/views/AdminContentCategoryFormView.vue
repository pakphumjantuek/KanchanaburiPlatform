<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  createContentCategory,
  getContentCategory,
  updateContentCategory,
} from '../api/adminContentCategoryApi'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'
import AppSelect from '@/components/common/input/AppSelect.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

const route = useRoute()
const router = useRouter()
const swal = useSwal()
const categoryId = computed(() => (typeof route.params.id === 'string' ? route.params.id : ''))
const isEdit = computed(() => Boolean(categoryId.value))
const loading = ref(isEdit.value)
const saving = ref(false)
const form = reactive({
  categoryName: '',
  description: '',
  status: 'Active' as 'Active' | 'Inactive',
})
const statusOptions = [
  { value: 'Active', label: 'เปิดใช้งาน (Active)' },
  { value: 'Inactive', label: 'ปิดใช้งาน (Inactive)' },
]

async function load() {
  if (!isEdit.value) return
  try {
    const category = await getContentCategory(categoryId.value)
    form.categoryName = category.categoryName
    form.description = category.description ?? ''
    form.status = category.status
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'ไม่พบหมวดหมู่ที่ต้องการ'))
    await router.replace('/admin/content-categories')
  } finally {
    loading.value = false
  }
}

async function save() {
  if (!form.categoryName.trim()) {
    await swal.warning('กรอกชื่อหมวดหมู่', 'กรุณาระบุชื่อหมวดหมู่คอนเทนต์')
    return
  }
  saving.value = true
  try {
    if (isEdit.value) await updateContentCategory(categoryId.value, form)
    else
      await createContentCategory({
        categoryName: form.categoryName,
        description: form.description,
      })
    await swal.success(isEdit.value ? 'บันทึกการแก้ไขเรียบร้อยแล้ว' : 'เพิ่มหมวดหมู่ใหม่เรียบร้อยแล้ว')
    await router.push('/admin/content-categories')
  } catch (error) {
    await swal.error('บันทึกไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}
onMounted(load)
</script>

<template>
  <main class="mx-auto w-full max-w-3xl px-6 py-8 lg:px-10 space-y-6">
    <!-- HEADER -->
    <div class="border-b border-slate-300 pb-6">
      <RouterLink
        to="/admin/content-categories"
        class="inline-flex items-center gap-1.5 text-sm font-bold text-teal-700 hover:text-teal-800 transition mb-3"
      >
        <i class="mdi mdi-arrow-left text-base" />
        <span>กลับไปหน้ารายการหมวดหมู่</span>
      </RouterLink>
      <div class="flex items-center gap-2">
        <span class="rounded-lg bg-teal-50 px-2.5 py-1 text-xs font-bold text-teal-800">
          {{ isEdit ? 'EDIT' : 'NEW' }}
        </span>
        <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-slate-900">
          {{ isEdit ? 'แก้ไขหมวดหมู่คอนเทนต์' : 'เพิ่มหมวดหมู่คอนเทนต์ใหม่' }}
        </h1>
      </div>
      <p class="mt-1 text-sm font-bold text-slate-600">
        กำหนดชื่อ คำอธิบาย และสถานะการเปิดใช้งานของหมวดหมู่บทความ
      </p>
    </div>

    <!-- FORM CARD -->
    <form
      v-if="!loading"
      class="space-y-6 rounded-2xl border border-slate-300 bg-white p-6 sm:p-8 shadow-xs"
      @submit.prevent="save"
    >
      <AppTextField
        v-model="form.categoryName"
        label="ชื่อหมวดหมู่ *"
        placeholder="เช่น แหล่งท่องเที่ยวเชิงวัฒนธรรม, ร้านอาหารและคาเฟ่"
        required
      />

      <AppTextarea
        v-model="form.description"
        label="รายละเอียด / คำอธิบายหมวดหมู่"
        placeholder="ระบุคำอธิบายขอบเขตของเนื้อหาในหมวดหมู่นี้เพื่อให้เข้าใจง่าย"
        rows="4"
      />

      <AppSelect
        v-if="isEdit"
        v-model="form.status"
        label="สถานะการใช้งาน"
        :items="statusOptions"
        item-title="label"
        item-value="value"
      />

      <div class="flex items-center justify-end gap-3 border-t border-slate-300 pt-6">
        <RouterLink
          to="/admin/content-categories"
          class="rounded-xl border border-slate-300 px-5 py-2.5 text-sm font-bold text-slate-700 hover:bg-slate-50 transition"
        >
          ยกเลิก
        </RouterLink>
        <button
          type="submit"
          class="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-6 py-2.5 text-sm font-bold text-white shadow-xs hover:bg-teal-800 transition disabled:opacity-50"
          :disabled="saving"
        >
          <i class="mdi mdi-content-save-outline text-lg" :class="{ 'animate-spin': saving }" />
          <span>{{ saving ? 'กำลังบันทึก...' : 'บันทึกข้อมูล' }}</span>
        </button>
      </div>
    </form>

    <div v-else class="h-48 flex items-center justify-center rounded-2xl border border-slate-300 bg-white">
      <div class="inline-flex items-center gap-2 text-slate-500 font-bold text-sm">
        <i class="mdi mdi-loading mdi-spin text-xl text-teal-700" />
        <span>กำลังโหลดข้อมูลหมวดหมู่...</span>
      </div>
    </div>
  </main>
</template>
