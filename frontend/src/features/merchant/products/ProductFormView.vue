<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  createProduct,
  getProduct,
  getProductCategories,
  updateProduct,
  uploadCoverImage,
  uploadDetailImages,
  type ProductCategory,
  type ProductFormData,
} from '@/features/shops/api'
import { getMyShop } from '@/features/merchant/api/shopApi'
import AppSelect from '@/components/common/input/AppSelect.vue'
import AppTextField from '@/components/common/input/AppTextField.vue'
import AppTextarea from '@/components/common/input/AppTextarea.vue'
import AppFileInput from '@/components/common/input/AppFileInput.vue'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import { imageUrl } from '@/shared/utils/imageUrl'

const route = useRoute()
const router = useRouter()
const categories = ref<ProductCategory[]>([])
const loading = ref(true)
const saving = ref(false)
const swal = useSwal()
const id = route.params.id as string | undefined

const form = ref<ProductFormData>({
  shopId: '',
  productCategoryId: null,
  productName: '',
  description: '',
  price: null,
  quantity: 0,
  imageUrl: '',
  status: 'Active',
})

const coverFile = ref<File | null>(null)
const detailFiles = ref<File[]>([])
const existingCoverUrl = ref<string | null>(null)
const existingDetailImages = ref<string[]>([])

// Local object URLs for instant reactive preview
const coverPreviewUrl = computed(() => {
  if (coverFile.value) {
    return URL.createObjectURL(coverFile.value)
  }
  if (existingCoverUrl.value) {
    return imageUrl(existingCoverUrl.value)
  }
  return null
})

const detailPreviews = computed(() => {
  return detailFiles.value.map((f) => ({
    name: f.name,
    size: (f.size / (1024 * 1024)).toFixed(2),
    url: URL.createObjectURL(f),
  }))
})

const selectedCategoryName = computed(() => {
  const cat = categories.value.find((c) => c.productCategoryId === form.value.productCategoryId)
  return cat ? cat.categoryName : 'เลือกหมวดหมู่สินค้า'
})

function removeCoverFile() {
  coverFile.value = null
}

function removeDetailFile(index: number) {
  detailFiles.value = detailFiles.value.filter((_, i) => i !== index)
}

async function submit() {
  if (
    !form.value.productName ||
    !form.value.productCategoryId ||
    form.value.price === null ||
    form.value.price < 0
  ) {
    await swal.warning('กรอกข้อมูลไม่ครบถ้วน', 'กรุณากรอกชื่อสินค้า หมวดหมู่ และราคาให้ถูกต้อง')
    return
  }
  saving.value = true
  try {
    const payload = { ...form.value, price: Number(form.value.price) }
    const productId = id ?? (await createProduct(payload)).productId
    if (id) await updateProduct(id, payload)
    if (coverFile.value) await uploadCoverImage(productId, coverFile.value)
    if (detailFiles.value.length) await uploadDetailImages(productId, detailFiles.value)
    await swal.success(id ? 'อัปเดตข้อมูลสินค้าเรียบร้อย' : 'เพิ่มสินค้าใหม่เรียบร้อยแล้ว')
    await router.push('/my-shop/products')
  } catch (error) {
    await swal.error('บันทึกสินค้าไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    const shop = await getMyShop()
    categories.value = await getProductCategories()
    form.value.shopId = shop.shopId
    if (id) {
      const product = await getProduct(id)
      if (product.shopId !== shop.shopId) {
        await router.replace('/my-shop/products')
        return
      }
      form.value = {
        shopId: product.shopId,
        productCategoryId: product.productCategoryId,
        productName: product.productName,
        description: product.description ?? '',
        price: product.price,
        quantity: product.quantity,
        imageUrl: product.imageUrl ?? '',
        status: product.status || 'Active',
      }
      existingCoverUrl.value = product.imageUrl ?? null
      existingDetailImages.value = product.detailImages ?? []
    }
  } catch (error) {
    await swal.error('โหลดข้อมูลไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
    await router.replace('/my-shop/products')
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="mx-auto w-full max-w-7xl space-y-6 pb-16 text-slate-800">
    <!-- Skeleton Loading -->
    <div v-if="loading" class="space-y-6">
      <div class="h-28 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 space-y-6">
          <div class="h-72 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
          <div class="h-48 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
        </div>
        <div class="h-96 w-full animate-pulse rounded-2xl bg-white border border-slate-200"></div>
      </div>
    </div>

    <form v-else class="space-y-6" @submit.prevent="submit">
      <!-- 1. Header Navigation Card -->
      <div
        class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs"
      >
        <div class="flex items-center gap-4">
          <div
            class="flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-600 text-white shadow-xs shrink-0"
          >
            <i
              :class="[
                'mdi text-2xl text-white',
                id ? 'mdi-pencil-box-outline' : 'mdi-plus-box-outline',
              ]"
            ></i>
          </div>
          <div>
            <div class="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-50 border border-emerald-200 text-xs font-bold text-emerald-800 mb-1">
              <i class="mdi mdi-tag-outline text-xs text-emerald-600"></i>
              <span>{{ id ? 'แก้ไขข้อมูลสินค้าเดิม' : 'สร้างสินค้าใหม่ลงร้านค้า' }}</span>
            </div>
            <h1 class="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
              {{ id ? 'แก้ไขข้อมูลสินค้า' : 'เพิ่มสินค้าใหม่' }}
            </h1>
            <p class="text-xs sm:text-sm text-slate-500 font-medium mt-0.5">
              กำหนดชื่อสินค้า หมวดหมู่ ราคาวางจำหน่าย คลังสต็อก และภาพประกอบสินค้า
            </p>
          </div>
        </div>

        <div class="flex items-center gap-3 self-start sm:self-auto">
          <RouterLink
            to="/my-shop/products"
            class="inline-flex items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 hover:bg-slate-100 px-4 py-2.5 text-xs sm:text-sm font-semibold text-slate-700 transition cursor-pointer select-none"
          >
            <i class="mdi mdi-arrow-left text-base text-slate-500"></i>
            <span>ย้อนกลับรายการสินค้า</span>
          </RouterLink>
        </div>
      </div>

      <!-- 2. Main Form Grid (2 Columns: Form Fields + Live Preview / Uploads) -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <!-- Left Column: Form Details (7 Columns) -->
        <div class="lg:col-span-7 space-y-6">
          <!-- Section 1: Basic Information -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <i class="mdi mdi-information-outline text-xl"></i>
              </span>
              <div>
                <h2 class="text-base font-bold text-slate-900">ข้อมูลพื้นฐานของสินค้า</h2>
                <p class="text-xs text-slate-500 font-medium">ระบุชื่อ หมวดหมู่ และสถานะการจำหน่าย</p>
              </div>
            </div>

            <div class="space-y-5">
              <AppTextField
                v-model="form.productName"
                label="ชื่อสินค้า *"
                placeholder="เช่น กล้วยตากพลังงานแสงอาทิตย์, ชาใบหม่อนกาญจนบุรี"
              />

              <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <AppSelect
                  v-model="form.productCategoryId"
                  label="หมวดหมู่สินค้า *"
                  :items="categories"
                  item-title="categoryName"
                  item-value="productCategoryId"
                  placeholder="เลือกหมวดหมู่สินค้า"
                  clearable
                />

                <!-- Status Selector -->
                <div>
                  <label class="block text-xs sm:text-sm font-bold text-slate-700 mb-1.5">
                    สถานะการวางจำหน่าย *
                  </label>
                  <div class="inline-flex w-full items-center rounded-full bg-slate-100 p-1 border-2 border-slate-300 shadow-inner">
                    <button
                      type="button"
                      class="flex-1 flex items-center justify-center gap-2 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                      :class="
                        form.status === 'Active'
                          ? 'bg-emerald-600 text-white shadow-sm'
                          : 'text-slate-700 hover:text-slate-900 bg-transparent'
                      "
                      @click="form.status = 'Active'"
                    >
                      <span
                        class="h-2 w-2 rounded-full"
                        :class="form.status === 'Active' ? 'bg-white shadow-2xs' : 'bg-emerald-600'"
                      ></span>
                      <span>พร้อมจำหน่าย</span>
                    </button>
                    <button
                      type="button"
                      class="flex-1 flex items-center justify-center gap-2 rounded-full py-2 px-3 text-xs font-bold transition-all duration-200 cursor-pointer select-none outline-none focus:outline-none focus-visible:outline-none active:outline-none"
                      :class="
                        form.status === 'Inactive'
                          ? 'bg-rose-600 text-white shadow-sm'
                          : 'text-slate-700 hover:text-slate-900 bg-transparent'
                      "
                      @click="form.status = 'Inactive'"
                    >
                      <span
                        class="h-2 w-2 rounded-full"
                        :class="form.status === 'Inactive' ? 'bg-white shadow-2xs' : 'bg-rose-500'"
                      ></span>
                      <span>ปิดการขายชั่วคราว</span>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Section 2: Pricing and Inventory -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-teal-50 text-teal-600">
                <i class="mdi mdi-cash-multiple text-xl"></i>
              </span>
              <div>
                <h2 class="text-base font-bold text-slate-900">ราคาและคลังสต็อก</h2>
                <p class="text-xs text-slate-500 font-medium">กำหนดราคาขายต่อหน่วยและจำนวนคงเหลือในคลัง</p>
              </div>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
              <div>
                <AppTextField
                  v-model="form.price"
                  label="ราคาจำหน่าย (บาท) *"
                  type="number"
                  min="0"
                  placeholder="เช่น 150"
                />
                <span class="text-[11px] text-slate-400 mt-1 block font-medium">
                  ราคาขายสุทธิที่ลูกค้าจะชำระ
                </span>
              </div>

              <div>
                <AppTextField
                  v-model="form.quantity"
                  label="จำนวนสินค้าในคลังสต็อก (ชิ้น) *"
                  type="number"
                  min="0"
                  placeholder="เช่น 50"
                />
                <div class="mt-1 flex items-center gap-1.5 text-[11px]">
                  <span
                    v-if="form.quantity === 0"
                    class="text-rose-600 font-bold"
                  >
                    สถานะ: สินค้าหมดสต็อก (Out of stock)
                  </span>
                  <span
                    v-else-if="form.quantity !== null && form.quantity <= 5"
                    class="text-amber-600 font-bold"
                  >
                    สถานะ: สต็อกเหลือน้อย
                  </span>
                  <span
                    v-else
                    class="text-emerald-600 font-bold"
                  >
                    สถานะ: พร้อมจำหน่ายปกติ
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- Section 3: Description -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-6">
            <div class="flex items-center gap-3 border-b border-slate-100 pb-4">
              <span class="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <i class="mdi mdi-text-box-outline text-xl"></i>
              </span>
              <div>
                <h2 class="text-base font-bold text-slate-900">รายละเอียดสินค้า</h2>
                <p class="text-xs text-slate-500 font-medium">
                  อธิบายจุดเด่น ส่วนผสม หรือคำแนะนำในการใช้งานสินค้า
                </p>
              </div>
            </div>

            <AppTextarea
              v-model="form.description"
              label="คำอธิบายรายละเอียดสินค้า"
              placeholder="ระบุจุดเด่น ส่วนประกอบ วิธีการเก็บรักษา หรือข้อมูลสำคัญที่ลูกค้าควรทราบ..."
              rows="5"
            />
          </div>
        </div>

        <!-- Right Column: Images & Live Preview (5 Columns) -->
        <div class="lg:col-span-5 space-y-6 lg:sticky lg:top-6">
          <!-- Section 4: Cover Image -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-5">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div class="flex items-center gap-2.5">
                <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
                  <i class="mdi mdi-image-outline text-lg"></i>
                </span>
                <h3 class="text-sm font-bold text-slate-900">รูปภาพหน้าปกหลัก</h3>
              </div>
              <span
                v-if="coverFile"
                class="inline-flex items-center gap-1 rounded-full bg-emerald-50 border border-emerald-200 px-2 py-0.5 text-[11px] font-bold text-emerald-700"
              >
                <i class="mdi mdi-check text-xs"></i>
                เลือกรูปใหม่แล้ว
              </span>
              <span
                v-else-if="existingCoverUrl"
                class="inline-flex items-center gap-1 rounded-full bg-slate-100 border border-slate-200 px-2 py-0.5 text-[11px] font-medium text-slate-600"
              >
                ใช้รูปเดิมในระบบ
              </span>
            </div>

            <!-- Cover Image Preview Frame -->
            <div
              class="relative aspect-video sm:aspect-4/3 w-full overflow-hidden rounded-2xl border-2 border-dashed border-slate-200 bg-slate-50 flex items-center justify-center group"
            >
              <img
                v-if="coverPreviewUrl"
                :src="coverPreviewUrl"
                alt="รูปหน้าปกสินค้า"
                class="h-full w-full object-cover"
              />
              <div v-else class="text-center p-6 space-y-2 text-slate-400">
                <i class="mdi mdi-cloud-upload-outline text-4xl block opacity-60"></i>
                <p class="text-xs font-semibold text-slate-500">ยังไม่ได้เลือกรูปภาพหน้าปก</p>
                <p class="text-[11px] text-slate-400">รองรับไฟล์ JPG, PNG หรือ WebP</p>
              </div>

              <!-- Clear newly selected cover image -->
              <button
                v-if="coverFile"
                type="button"
                aria-label="ลบรูปภาพที่เลือก"
                class="absolute top-2.5 right-2.5 flex h-8 w-8 items-center justify-center rounded-xl bg-slate-900/70 text-white hover:bg-rose-600 transition backdrop-blur-xs cursor-pointer"
                @click="removeCoverFile"
              >
                <i class="mdi mdi-close text-lg"></i>
              </button>
            </div>

            <AppFileInput
              :label="existingCoverUrl ? 'เปลี่ยนรูปภาพหน้าปก' : 'อัปโหลดรูปภาพหน้าปก'"
              @change="coverFile = $event[0] ?? null"
            />
          </div>

          <!-- Section 5: Detail Images Gallery -->
          <div class="rounded-2xl border border-slate-200/80 bg-white p-6 shadow-xs space-y-5">
            <div class="flex items-center justify-between border-b border-slate-100 pb-3">
              <div class="flex items-center gap-2.5">
                <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-purple-50 text-purple-600">
                  <i class="mdi mdi-image-multiple text-lg"></i>
                </span>
                <h3 class="text-sm font-bold text-slate-900">รูปภาพรายละเอียดเพิ่มเติม</h3>
              </div>
              <span class="text-xs font-medium text-slate-500">
                สูงสุด 8 รูป
              </span>
            </div>

            <!-- Existing detail images -->
            <div v-if="existingDetailImages.length > 0" class="space-y-2">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-slate-600">
                  รูปเดิมในระบบ ({{ existingDetailImages.length }} รูป)
                </span>
              </div>
              <div class="grid grid-cols-4 gap-2">
                <div
                  v-for="(img, index) in existingDetailImages"
                  :key="index"
                  class="relative aspect-square overflow-hidden rounded-xl border border-slate-200 bg-slate-100"
                >
                  <img
                    :src="imageUrl(img)"
                    :alt="`รูปรายละเอียด ${index + 1}`"
                    class="h-full w-full object-cover"
                  />
                  <span
                    class="absolute bottom-1 right-1 rounded-md bg-slate-900/70 px-1 py-0.2 text-[9px] font-bold text-white"
                  >
                    #{{ index + 1 }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Newly selected detail images preview -->
            <div v-if="detailPreviews.length > 0" class="space-y-2">
              <div class="flex items-center justify-between">
                <span class="text-xs font-bold text-emerald-700">
                  รูปใหม่ที่เลือก ({{ detailPreviews.length }} รูป)
                </span>
              </div>
              <div class="grid grid-cols-4 gap-2">
                <div
                  v-for="(file, index) in detailPreviews"
                  :key="index"
                  class="relative aspect-square overflow-hidden rounded-xl border border-emerald-300 bg-emerald-50 group"
                >
                  <img
                    :src="file.url"
                    :alt="file.name"
                    class="h-full w-full object-cover"
                  />
                  <button
                    type="button"
                    aria-label="ลบรูปนี้"
                    class="absolute top-1 right-1 flex h-6 w-6 items-center justify-center rounded-lg bg-slate-900/70 text-white hover:bg-rose-600 transition"
                    @click="removeDetailFile(index)"
                  >
                    <i class="mdi mdi-close text-xs"></i>
                  </button>
                  <span
                    class="absolute bottom-1 right-1 rounded bg-slate-900/70 px-1 text-[8px] font-bold text-white"
                  >
                    {{ file.size }}MB
                  </span>
                </div>
              </div>
            </div>

            <AppFileInput
              label="เลือกรูปภาพรายละเอียดเพิ่มเติม (สูงสุด 8 รูป)"
              multiple
              @change="detailFiles = $event.slice(0, 8)"
            />
          </div>

          <!-- Section 6: Mini Live Preview Card (WOW factor) -->
          <div class="rounded-2xl border border-slate-200/80 bg-slate-50/70 p-5 space-y-3">
            <div class="flex items-center gap-2 text-xs font-bold text-slate-500 uppercase tracking-wider">
              <i class="mdi mdi-eye-outline text-slate-600"></i>
              <span>ตัวอย่างการแสดงผลบนหน้าร้าน</span>
            </div>

            <div class="rounded-2xl border border-slate-200 bg-white p-3.5 shadow-xs flex items-center gap-3.5">
              <div class="h-16 w-16 rounded-xl bg-slate-100 overflow-hidden shrink-0 border border-slate-200/80 flex items-center justify-center">
                <img
                  v-if="coverPreviewUrl"
                  :src="coverPreviewUrl"
                  alt="รูปจำลอง"
                  class="h-full w-full object-cover"
                />
                <i v-else class="mdi mdi-image-outline text-slate-300 text-2xl"></i>
              </div>

              <div class="min-w-0 flex-1">
                <span class="text-[10px] font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md inline-block mb-0.5">
                  {{ selectedCategoryName }}
                </span>
                <p class="text-sm font-bold text-slate-900 truncate">
                  {{ form.productName || 'ระบุชื่อสินค้าของคุณ' }}
                </p>
                <div class="flex items-center gap-2 mt-1">
                  <span class="text-sm font-black text-emerald-700">
                    ฿{{ form.price !== null ? Number(form.price).toLocaleString() : '0' }}
                  </span>
                  <span class="text-[11px] text-slate-400">
                    คลัง: {{ form.quantity }} ชิ้น
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 3. Form Action Bottom Bar -->
      <div
        class="sticky bottom-4 z-20 flex items-center justify-between gap-3 rounded-2xl border border-slate-200/90 bg-white/95 p-4 shadow-lg backdrop-blur-md"
      >
        <RouterLink
          to="/my-shop/products"
          class="rounded-xl border border-slate-200 bg-white px-5 py-2.5 text-xs sm:text-sm font-bold text-slate-600 hover:bg-slate-50 hover:text-slate-800 transition"
        >
          ยกเลิก
        </RouterLink>

        <div class="flex items-center gap-3">
          <button
            type="submit"
            class="inline-flex items-center justify-center gap-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 px-6 py-2.5 text-xs sm:text-sm font-bold text-white shadow-xs transition active:scale-95 disabled:opacity-50 cursor-pointer border border-emerald-600"
            :disabled="saving"
          >
            <i
              class="mdi text-base text-white"
              :class="saving ? 'mdi-loading animate-spin' : 'mdi-content-save-check-outline'"
            ></i>
            <span class="text-white">{{
              saving ? 'กำลังบันทึกข้อมูล...' : id ? 'บันทึกการแก้ไขสินค้า' : 'ยืนยันเพิ่มสินค้าใหม่'
            }}</span>
          </button>
        </div>
      </div>
    </form>
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
