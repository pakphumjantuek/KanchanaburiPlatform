<script setup lang="ts">
import { ref } from 'vue'
import { useSwal } from '@/plugins/sweetalert'

interface Props {
  paying: boolean
  totalAmount: number
}

defineProps<Props>()

const emit = defineEmits<{
  submitSlip: [file: File, transferTime: string]
}>()

const swal = useSwal()

const selectedFile = ref<File | null>(null)
const previewUrl = ref<string>('')
const transferTime = ref<string>(new Date().toISOString().slice(0, 16))
const isCopied = ref(false)

const bankAccount = {
  bankName: 'ธนาคารกสิกรไทย (KBank)',
  accountName: 'บจก. กาญจนบุรี แพลตฟอร์ม ชุมชน',
  accountNumber: '123-4-56789-0',
  promptpayId: '080-000-0000',
}

function copyAccount() {
  navigator.clipboard.writeText(bankAccount.accountNumber)
  isCopied.value = true
  setTimeout(() => {
    isCopied.value = false
  }, 2000)
}

function handleFileChange(event: Event) {
  const target = event.target as HTMLInputElement
  if (!target.files || !target.files[0]) return
  setFile(target.files[0])
}

function handleDrop(event: DragEvent) {
  if (!event.dataTransfer?.files || !event.dataTransfer.files[0]) return
  setFile(event.dataTransfer.files[0])
}

function setFile(file: File) {
  const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp']
  const allowedExtensions = ['jpg', 'jpeg', 'png', 'webp']
  const maxFileSize = 5 * 1024 * 1024
  const extension = file.name.split('.').pop()?.toLowerCase() ?? ''
  const hasSupportedType = allowedTypes.includes(file.type) || allowedExtensions.includes(extension)

  if (!hasSupportedType || file.size > maxFileSize) {
    void swal.error('ไฟล์ไม่ถูกต้อง', 'รองรับไฟล์ JPG, PNG หรือ WEBP ขนาดไม่เกิน 5 MB')
    return
  }
  selectedFile.value = file
  previewUrl.value = URL.createObjectURL(file)
}

function clearFile() {
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value)
  }
  selectedFile.value = null
  previewUrl.value = ''
}

function onSubmit() {
  if (!selectedFile.value) {
    void swal.warning('กรุณาแนบสลิป', 'กรุณาอัปโหลดรูปภาพสลิปการโอนเงินก่อนทำการยืนยัน')
    return
  }
  emit('submitSlip', selectedFile.value, transferTime.value)
}
</script>

<template>
  <div class="space-y-6">
    <!-- Step 1: transfer -->
    <div class="space-y-4 rounded-2xl border border-[#E8D9C9] bg-white p-5">
      <div class="flex items-center gap-3">
        <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-sm font-black text-white">1</span>
        <div>
          <h3 class="text-sm font-black text-[#332820] sm:text-base">โอนเงินเข้าบัญชีนี้</h3>
          <p class="text-xs font-semibold text-[#786B62]">
            ยอดที่ต้องโอน
            <span class="text-base font-black text-[#D96C2C]">฿{{ totalAmount.toLocaleString('th-TH') }}</span>
          </p>
        </div>
      </div>

      <div class="space-y-3 rounded-2xl bg-[#FFF9F2] p-4">
        <div class="flex items-center gap-3">
          <span class="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-emerald-600 text-xl text-white">
            <i class="mdi mdi-bank"></i>
          </span>
          <div class="min-w-0">
            <p class="text-sm font-black text-[#332820]">{{ bankAccount.bankName }}</p>
            <p class="truncate text-xs font-semibold text-[#786B62]">{{ bankAccount.accountName }}</p>
          </div>
        </div>

        <div class="flex items-center justify-between gap-3 rounded-xl border border-[#D96C2C]/30 bg-white p-3">
          <div class="min-w-0">
            <span class="block text-[11px] font-semibold text-[#786B62]">เลขที่บัญชี</span>
            <span class="font-mono text-lg font-black tracking-wider text-[#D96C2C]">{{ bankAccount.accountNumber }}</span>
          </div>
          <button
            type="button"
            class="flex shrink-0 cursor-pointer items-center gap-1 rounded-full px-4 py-2 text-xs font-black text-white transition active:scale-95"
            :class="isCopied ? 'bg-emerald-600' : 'bg-[#D96C2C] hover:bg-[#BF5720]'"
            @click="copyAccount"
          >
            <i class="mdi text-white" :class="isCopied ? 'mdi-check' : 'mdi-content-copy'"></i>
            <span class="text-white">{{ isCopied ? 'คัดลอกแล้ว' : 'คัดลอก' }}</span>
          </button>
        </div>

        <p class="flex items-center justify-between px-1 text-xs font-semibold text-[#786B62]">
          <span>หรือโอนผ่านพร้อมเพย์</span>
          <span class="font-black text-[#332820]">{{ bankAccount.promptpayId }}</span>
        </p>
      </div>
    </div>

    <!-- Step 2: slip -->
    <div class="space-y-4 rounded-2xl border border-[#E8D9C9] bg-white p-5">
      <div class="flex items-center gap-3">
        <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-[#D96C2C] text-sm font-black text-white">2</span>
        <div>
          <h3 class="text-sm font-black text-[#332820] sm:text-base">แนบรูปสลิปการโอน</h3>
          <p class="text-xs font-semibold text-[#786B62]">ถ่ายหรือบันทึกภาพสลิปจากแอปธนาคาร แล้วอัปโหลดที่นี่</p>
        </div>
      </div>

      <div
        v-if="!previewUrl"
        class="group relative cursor-pointer rounded-2xl border-2 border-dashed border-[#E8D9C9] bg-[#FFF9F2] p-8 text-center transition hover:border-[#D96C2C] hover:bg-[#FFF1E8]"
        @dragover.prevent
        @drop.prevent="handleDrop"
      >
        <input
          type="file"
          accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
          class="absolute inset-0 z-10 h-full w-full cursor-pointer opacity-0"
          aria-label="อัปโหลดสลิป"
          @change="handleFileChange"
        />
        <div class="pointer-events-none space-y-2">
          <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-white text-3xl text-[#D96C2C] shadow-sm transition group-hover:scale-110">
            <i class="mdi mdi-cloud-upload-outline"></i>
          </div>
          <p class="text-sm font-black text-[#332820]">คลิก หรือลากรูปสลิปมาวางที่นี่</p>
          <p class="text-xs font-semibold text-[#786B62]">รองรับ JPG, PNG, WEBP ขนาดไม่เกิน 5 MB</p>
        </div>
      </div>

      <div v-else class="space-y-3 rounded-2xl border-2 border-[#D96C2C] bg-[#FFF9F2] p-3">
        <div class="mx-auto max-h-72 overflow-hidden rounded-xl bg-white shadow-sm">
          <img :src="previewUrl" alt="รูปสลิปการโอนเงิน" class="mx-auto max-h-72 object-contain" />
        </div>
        <div class="flex items-center justify-between">
          <span class="flex items-center gap-1 text-xs font-black text-emerald-800">
            <i class="mdi mdi-check-circle text-base text-emerald-600"></i>
            แนบสลิปเรียบร้อยแล้ว
          </span>
          <button
            type="button"
            class="flex cursor-pointer items-center gap-1 rounded-full px-3 py-1.5 text-xs font-black text-rose-600 transition hover:bg-rose-50"
            @click="clearFile"
          >
            <i class="mdi mdi-delete-outline"></i>
            <span>เปลี่ยนรูป</span>
          </button>
        </div>
      </div>

      <div class="space-y-1.5">
        <label class="block text-xs font-black text-[#332820]" for="transfer-time">วันและเวลาที่โอน (ตามสลิป)</label>
        <div class="flex items-center gap-2 rounded-xl border border-[#E8D9C9] bg-white px-3.5 py-2.5 transition focus-within:border-[#D96C2C] focus-within:ring-4 focus-within:ring-[#D96C2C]/10">
          <i class="mdi mdi-clock-outline text-lg text-[#D96C2C]"></i>
          <input
            id="transfer-time"
            v-model="transferTime"
            type="datetime-local"
            class="w-full bg-transparent text-sm font-bold text-[#332820] focus:outline-none"
          />
        </div>
      </div>
    </div>

    <button
      type="button"
      class="flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] py-4 text-base font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:-translate-y-0.5 focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-95 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:translate-y-0"
      :disabled="paying || !selectedFile"
      @click="onSubmit"
    >
      <i class="mdi text-xl text-white" :class="paying ? 'mdi-loading mdi-spin' : 'mdi-file-document-check-outline'"></i>
      <span class="text-white">{{ paying ? 'กำลังส่งหลักฐาน...' : 'แจ้งชำระเงิน' }}</span>
    </button>
    <p v-if="!selectedFile" class="-mt-3 text-center text-xs font-semibold text-[#786B62]">
      แนบสลิปก่อน แล้วปุ่มจะกดได้
    </p>
  </div>
</template>
