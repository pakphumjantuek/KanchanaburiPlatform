<script setup lang="ts">
import { reactive, ref } from 'vue'
import {
  createUserAddress,
  deleteUserAddress,
  setDefaultUserAddress,
  updateUserAddress,
  type SaveUserAddress,
  type UserAddress,
} from '@/features/cart/api/userAddressApi'
import { getApiErrorMessage } from '@/features/auth/api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'

interface Props {
  addresses: UserAddress[]
}

defineProps<Props>()

const emit = defineEmits<{
  changed: []
}>()

const swal = useSwal()
const saving = ref(false)
const editingAddressId = ref<string | null>(null)
const showForm = ref(false)

function emptyForm(): SaveUserAddress {
  return {
    recipientName: '',
    recipientPhone: '',
    addressLine: '',
    subDistrict: '',
    district: '',
    province: '',
    postalCode: '',
    isDefault: false,
  }
}

const form = reactive<SaveUserAddress>(emptyForm())

function resetForm() {
  Object.assign(form, emptyForm())
  editingAddressId.value = null
}

function openCreateForm() {
  resetForm()
  showForm.value = true
}

function openEditForm(address: UserAddress) {
  Object.assign(form, {
    recipientName: address.recipientName,
    recipientPhone: address.recipientPhone,
    addressLine: address.addressLine,
    subDistrict: address.subDistrict ?? '',
    district: address.district ?? '',
    province: address.province ?? '',
    postalCode: address.postalCode ?? '',
    isDefault: address.isDefault,
  })
  editingAddressId.value = address.userAddressId
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  resetForm()
}

async function saveAddress() {
  if (!form.recipientName.trim() || !form.recipientPhone.trim() || !form.addressLine.trim()) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณาระบุชื่อผู้รับ เบอร์โทรศัพท์ และรายละเอียดที่อยู่')
    return
  }

  saving.value = true
  try {
    const payload: SaveUserAddress = {
      ...form,
      recipientName: form.recipientName.trim(),
      recipientPhone: form.recipientPhone.trim(),
      addressLine: form.addressLine.trim(),
      subDistrict: form.subDistrict?.trim() || undefined,
      district: form.district?.trim() || undefined,
      province: form.province?.trim() || undefined,
      postalCode: form.postalCode?.trim() || undefined,
    }

    if (editingAddressId.value) {
      await updateUserAddress(editingAddressId.value, payload)
      await swal.success('แก้ไขที่อยู่เรียบร้อยแล้ว')
    } else {
      await createUserAddress(payload)
      await swal.success('เพิ่มที่อยู่เรียบร้อยแล้ว')
    }

    closeForm()
    emit('changed')
  } catch (error) {
    await swal.error('บันทึกที่อยู่ไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  } finally {
    saving.value = false
  }
}

async function setDefault(address: UserAddress) {
  if (address.isDefault) return

  try {
    await setDefaultUserAddress(address.userAddressId)
    emit('changed')
    await swal.success('ตั้งเป็นที่อยู่เริ่มต้นแล้ว')
  } catch (error) {
    await swal.error('ตั้งค่าที่อยู่เริ่มต้นไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}

async function removeAddress(address: UserAddress) {
  const confirmation = await swal.confirm(
    'ลบที่อยู่นี้หรือไม่?',
    `ที่อยู่ของ ${address.recipientName} จะถูกลบออกจากสมุดที่อยู่`,
  )
  if (!confirmation.isConfirmed) return

  try {
    await deleteUserAddress(address.userAddressId)
    emit('changed')
    await swal.success('ลบที่อยู่เรียบร้อยแล้ว')
  } catch (error) {
    await swal.error('ลบที่อยู่ไม่สำเร็จ', getApiErrorMessage(error, 'กรุณาลองใหม่อีกครั้ง'))
  }
}
</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#E8D9C9] pb-4">
      <div>
        <div class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-[#D96C2C]/15 text-[#D96C2C]">
            <i class="mdi mdi-map-marker-radius-outline text-lg"></i>
          </span>
          <h2 class="text-xl sm:text-2xl font-black text-[#332820]">
            ที่อยู่จัดส่งของฉัน
          </h2>
          <span
            v-if="addresses.length > 0"
            class="rounded-full bg-[#D96C2C]/15 text-[#D96C2C] border border-[#D96C2C]/30 text-xs font-black px-2.5 py-0.5 ml-1"
          >
            {{ addresses.length }} แห่ง
          </span>
        </div>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold mt-1">
          บันทึกและจัดการที่อยู่สำหรับการจัดส่งสินค้าและของฝากจากเมืองกาญจน์
        </p>
      </div>

      <button
        v-if="!showForm"
        type="button"
        class="inline-flex items-center justify-center gap-2 rounded-2xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] hover:from-[#BF5720] hover:to-[#a84614] px-5 py-2.5 text-xs sm:text-sm font-black text-white shadow-md shadow-[#D96C2C]/25 transition active:scale-95 cursor-pointer border border-[#D96C2C] shrink-0"
        @click="openCreateForm"
      >
        <i class="mdi mdi-plus-circle text-base"></i>
        <span>เพิ่มที่อยู่ใหม่</span>
      </button>
    </div>

    <!-- Create / Edit Form Modal-Card -->
    <Transition name="tab-fade">
      <form
        v-if="showForm"
        class="relative overflow-hidden rounded-2xl border-2 border-[#D96C2C]/40 bg-[#FFF7EF] p-5 sm:p-6 shadow-md space-y-4"
        @submit.prevent="saveAddress"
      >
        <div class="flex items-center justify-between gap-4 border-b border-[#E8D9C9] pb-3">
          <div class="flex items-center gap-2">
            <span class="flex h-7 w-7 items-center justify-center rounded-lg bg-[#D96C2C] text-white text-sm">
              <i :class="editingAddressId ? 'mdi mdi-pencil' : 'mdi mdi-plus'"></i>
            </span>
            <h3 class="font-black text-[#332820] text-sm sm:text-base">
              {{ editingAddressId ? 'แก้ไขข้อมูลที่อยู่จัดส่ง' : 'เพิ่มที่อยู่จัดส่งใหม่' }}
            </h3>
          </div>
          <button
            type="button"
            class="flex h-8 w-8 items-center justify-center rounded-xl bg-white border border-[#E8D9C9] text-[#786B62] hover:text-[#D96C2C] hover:border-[#D96C2C] transition cursor-pointer"
            aria-label="ปิดฟอร์ม"
            @click="closeForm"
          >
            <i class="mdi mdi-close text-lg"></i>
          </button>
        </div>

        <div class="grid gap-4 sm:grid-cols-2">
          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#332820]">
              ชื่อ-นามสกุล ผู้รับ <span class="text-rose-500">*</span>
            </label>
            <input
              v-model="form.recipientName"
              maxlength="150"
              placeholder="ระบุชื่อผู้รับพัสดุ"
              required
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3.5 py-2.5 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
            />
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#332820]">
              เบอร์โทรศัพท์ <span class="text-rose-500">*</span>
            </label>
            <input
              v-model="form.recipientPhone"
              maxlength="30"
              inputmode="tel"
              placeholder="08X-XXX-XXXX"
              required
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3.5 py-2.5 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
            />
          </div>
        </div>

        <div class="space-y-1.5">
          <label class="block text-xs font-black text-[#332820]">
            บ้านเลขที่ / ซอย / ถนน / อาคาร / หมู่บ้าน <span class="text-rose-500">*</span>
          </label>
          <textarea
            v-model="form.addressLine"
            maxlength="500"
            rows="2"
            placeholder="ระบุบ้านเลขที่ ซอย ถนน แขวง/ตำบล หรือจุดสังเกต"
            required
            class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3.5 py-2.5 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
          ></textarea>
        </div>

        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-[#786B62]">ตำบล / แขวง</label>
            <input
              v-model="form.subDistrict"
              placeholder="ตำบล/แขวง"
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3 py-2 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C]"
            />
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-[#786B62]">อำเภอ / เขต</label>
            <input
              v-model="form.district"
              placeholder="อำเภอ/เขต"
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3 py-2 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C]"
            />
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-[#786B62]">จังหวัด</label>
            <input
              v-model="form.province"
              placeholder="จังหวัด"
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3 py-2 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C]"
            />
          </div>
          <div class="space-y-1.5">
            <label class="block text-xs font-bold text-[#786B62]">รหัสไปรษณีย์</label>
            <input
              v-model="form.postalCode"
              maxlength="10"
              inputmode="numeric"
              placeholder="รหัสไปรษณีย์"
              class="w-full rounded-xl border-2 border-[#E8D9C9] bg-white px-3 py-2 text-xs sm:text-sm font-bold text-[#332820] outline-none transition focus:border-[#D96C2C]"
            />
          </div>
        </div>

        <div class="pt-2 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <label class="flex cursor-pointer items-center gap-2.5 text-xs font-bold text-[#332820] select-none">
            <input
              v-model="form.isDefault"
              type="checkbox"
              class="h-4 w-4 rounded accent-[#D96C2C]"
            />
            <span>กำหนดให้เป็นที่อยู่จัดส่งเริ่มต้น</span>
          </label>

          <div class="flex items-center justify-end gap-2.5">
            <button
              type="button"
              class="rounded-xl px-4 py-2.5 text-xs font-black text-[#786B62] hover:bg-[#E8D9C9]/50 transition cursor-pointer"
              @click="closeForm"
            >
              ยกเลิก
            </button>
            <button
              type="submit"
              :disabled="saving"
              class="inline-flex items-center gap-1.5 rounded-xl bg-gradient-to-r from-[#D96C2C] to-[#BF5720] hover:from-[#BF5720] hover:to-[#a84614] px-5 py-2.5 text-xs font-black text-white shadow-md shadow-[#D96C2C]/25 transition active:scale-95 disabled:opacity-60 cursor-pointer"
            >
              <i v-if="saving" class="mdi mdi-loading animate-spin text-sm"></i>
              <i v-else class="mdi mdi-content-save-outline text-sm"></i>
              <span>{{ saving ? 'กำลังบันทึก...' : 'บันทึกที่อยู่' }}</span>
            </button>
          </div>
        </div>
      </form>
    </Transition>

    <!-- Empty State -->
    <div
      v-if="!addresses.length"
      class="text-center py-16 px-4 rounded-2xl bg-[#F7F0E6] border-2 border-dashed border-[#E8D9C9] space-y-3"
    >
      <div class="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl bg-[#D96C2C]/15 text-[#D96C2C]">
        <i class="mdi mdi-map-marker-plus-outline text-3xl"></i>
      </div>
      <h3 class="text-base font-black text-[#332820]">ยังไม่มีที่อยู่จัดส่งที่บันทึกไว้</h3>
      <p class="text-xs text-[#786B62] max-w-sm mx-auto font-semibold">
        เพิ่มที่อยู่ของคุณเพื่อให้ขั้นตอนการสั่งซื้อสินค้าและของฝากกาญจนบุรีรวดเร็วและสะดวกยิ่งขึ้น
      </p>
      <div class="pt-2">
        <button
          type="button"
          class="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#D96C2C] text-white font-black text-xs shadow-md transition hover:bg-[#BF5720] cursor-pointer"
          @click="openCreateForm"
        >
          <i class="mdi mdi-plus text-sm"></i>
          <span>เพิ่มที่อยู่แรกของคุณ</span>
        </button>
      </div>
    </div>

    <!-- Address Cards List -->
    <div v-else class="space-y-3.5">
      <article
        v-for="address in addresses"
        :key="address.userAddressId"
        class="group relative overflow-hidden rounded-2xl border-2 bg-white p-5 transition-all duration-200"
        :class="
          address.isDefault
            ? 'border-[#D96C2C]/60 shadow-sm ring-2 ring-[#D96C2C]/10'
            : 'border-[#E8D9C9] hover:border-[#D96C2C]/40 hover:shadow-2xs'
        "
      >
        <div class="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
          <!-- Address Info -->
          <div class="space-y-2 min-w-0 flex-1">
            <div class="flex flex-wrap items-center gap-2">
              <span class="font-black text-[#332820] text-sm sm:text-base flex items-center gap-1.5">
                <i class="mdi mdi-account text-[#D96C2C]"></i>
                {{ address.recipientName }}
              </span>
              <span class="text-xs text-[#786B62] font-semibold flex items-center gap-1">
                <i class="mdi mdi-phone text-xs text-[#786B62]/70"></i>
                {{ address.recipientPhone }}
              </span>
              <span
                v-if="address.isDefault"
                class="inline-flex items-center gap-1 rounded-full border border-emerald-300 bg-emerald-50 px-2.5 py-0.5 text-[10px] font-black text-emerald-800"
              >
                <i class="mdi mdi-check-circle text-xs text-emerald-600"></i>
                ที่อยู่เริ่มต้น
              </span>
            </div>

            <p class="text-xs text-[#786B62] font-semibold leading-relaxed flex items-start gap-1.5">
              <i class="mdi mdi-map-marker text-sm text-[#D96C2C] shrink-0 mt-0.5"></i>
              <span>
                {{ [address.addressLine, address.subDistrict, address.district, address.province, address.postalCode].filter(Boolean).join(' ') }}
              </span>
            </p>
          </div>

          <!-- Action Buttons -->
          <div class="flex flex-wrap items-center gap-2 shrink-0">
            <button
              v-if="!address.isDefault"
              type="button"
              class="inline-flex items-center gap-1 rounded-xl border border-[#D96C2C]/30 bg-white px-3 py-1.5 text-xs font-black text-[#D96C2C] hover:bg-[#D96C2C]/10 transition cursor-pointer"
              @click="setDefault(address)"
            >
              <i class="mdi mdi-check text-xs"></i>
              <span>ตั้งเป็นค่าเริ่มต้น</span>
            </button>

            <button
              type="button"
              class="inline-flex items-center gap-1 rounded-xl border border-[#E8D9C9] bg-white px-3 py-1.5 text-xs font-black text-[#786B62] hover:bg-[#F7F0E6] hover:text-[#332820] transition cursor-pointer"
              @click="openEditForm(address)"
            >
              <i class="mdi mdi-pencil-outline text-xs"></i>
              <span>แก้ไข</span>
            </button>

            <button
              type="button"
              class="inline-flex items-center gap-1 rounded-xl border border-rose-200 bg-rose-50/50 px-3 py-1.5 text-xs font-black text-rose-600 hover:bg-rose-100 transition cursor-pointer"
              @click="removeAddress(address)"
            >
              <i class="mdi mdi-delete-outline text-xs"></i>
              <span>ลบ</span>
            </button>
          </div>
        </div>
      </article>
    </div>
  </div>
</template>
