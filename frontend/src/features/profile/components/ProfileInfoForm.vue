<script setup lang="ts">

interface Props {
  profile: {
    firstName: string
    lastName: string
    email: string
    role: string
  }
  saving: boolean
}

const props = defineProps<Props>()

const emit = defineEmits<{
  'save': []
}>()
</script>

<template>
  <div class="rounded-3xl border border-[#E8D9C9] bg-white p-6 sm:p-8 shadow-xs space-y-6">
    <!-- Section Title -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[#E8D9C9] pb-4">
      <div>
        <div class="flex items-center gap-2">
          <span class="flex h-8 w-8 items-center justify-center rounded-xl bg-[#D96C2C]/15 text-[#D96C2C]">
            <i class="mdi mdi-card-account-details-outline text-lg"></i>
          </span>
          <h2 class="text-xl sm:text-2xl font-black text-[#332820]">
            ข้อมูลส่วนตัวของฉัน
          </h2>
        </div>
        <p class="text-xs sm:text-sm text-[#786B62] font-semibold mt-1">
          จัดการข้อมูลส่วนบุคคล อัปเดตชื่อและนามสกุลของคุณเพื่อให้แสดงผลบนแพลตฟอร์มได้อย่างถูกต้อง
        </p>
      </div>

      <span class="inline-flex items-center gap-1.5 self-start sm:self-auto px-3 py-1 rounded-full text-xs font-black bg-emerald-100 text-emerald-800 border border-emerald-300">
        <i class="mdi mdi-shield-check text-sm"></i>
        <span>บัญชียืนยันแล้ว</span>
      </span>
    </div>

    <!-- Where the name is used -->
    <div class="flex items-start gap-3 rounded-2xl border border-[#D96C2C]/20 bg-[#FFF1E8] p-4">
      <span class="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-white text-lg text-[#D96C2C]">
        <i class="mdi mdi-account-eye-outline"></i>
      </span>
      <div class="min-w-0">
        <p class="text-sm font-black text-[#BF5720]">ชื่อของคุณจะแสดงที่ไหนบ้าง?</p>
        <p class="mt-0.5 text-xs font-semibold leading-relaxed text-[#786B62] sm:text-sm">
          ชื่อและนามสกุลจะแสดงร่วมกับบทความ รีวิว และคำสั่งซื้อของคุณบนแพลตฟอร์ม
          อีเมลใช้เข้าสู่ระบบและเปลี่ยนเองไม่ได้
        </p>
      </div>
    </div>

    <!-- Personal Info Form -->
    <form class="space-y-5 pt-2" @submit.prevent="emit('save')">
      <!-- Edit Section: First & Last Name -->
      <div class="space-y-3">
        <div class="flex items-center gap-1.5 text-xs font-black text-[#786B62] uppercase tracking-wider">
          <i class="mdi mdi-pencil-outline text-[#D96C2C]"></i>
          <span>ข้อมูลที่สามารถแก้ไขได้</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#332820]">
              ชื่อจริง <span class="text-rose-500">*</span>
            </label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-[#786B62]">
                <i class="mdi mdi-account text-base"></i>
              </span>
              <input
                v-model="props.profile.firstName"
                type="text"
                placeholder="พิมพ์ชื่อจริงของคุณ"
                required
                maxlength="50"
                class="w-full rounded-2xl border border-[#E8D9C9] bg-white pl-10 pr-4 py-3 text-sm font-bold text-[#332820] placeholder-[#786B62]/50 outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
              />
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#332820]">
              นามสกุล <span class="text-rose-500">*</span>
            </label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-[#786B62]">
                <i class="mdi mdi-account text-base"></i>
              </span>
              <input
                v-model="props.profile.lastName"
                type="text"
                placeholder="พิมพ์นามสกุลของคุณ"
                required
                maxlength="50"
                class="w-full rounded-2xl border border-[#E8D9C9] bg-white pl-10 pr-4 py-3 text-sm font-bold text-[#332820] placeholder-[#786B62]/50 outline-none transition focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/10"
              />
            </div>
          </div>
        </div>
      </div>

      <!-- Readonly Section: Email & Role -->
      <div class="space-y-3 pt-3 border-t border-[#E8D9C9]/60">
        <div class="flex items-center gap-1.5 text-xs font-black text-[#786B62] uppercase tracking-wider">
          <i class="mdi mdi-lock-outline text-[#786B62]"></i>
          <span>ข้อมูลระบบ (อ่านอย่างเดียว)</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#786B62]">
              อีเมลบัญชีผู้ใช้ (ใช้เข้าสู่ระบบ)
            </label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-[#786B62]/70">
                <i class="mdi mdi-email text-base"></i>
              </span>
              <input
                :value="props.profile.email"
                type="email"
                disabled
                class="w-full rounded-2xl border border-[#E8D9C9]/80 bg-[#F7F0E6]/80 pl-10 pr-10 py-3 text-sm font-bold text-[#786B62] cursor-not-allowed select-none"
              />
              <span class="absolute inset-y-0 right-0 pr-3.5 flex items-center pointer-events-none text-[#786B62]/60">
                <i class="mdi mdi-lock text-sm"></i>
              </span>
            </div>
          </div>

          <div class="space-y-1.5">
            <label class="block text-xs font-black text-[#786B62]">
              ระดับสถานะสมาชิก
            </label>
            <div class="relative">
              <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center pointer-events-none text-[#786B62]/70">
                <i class="mdi mdi-shield-account text-base"></i>
              </span>
              <input
                :value="props.profile.role === 'Admin' ? 'ผู้ดูแลระบบ (Administrator)' : 'สมาชิกทั่วไป (Member)'"
                type="text"
                disabled
                class="w-full rounded-2xl border border-[#E8D9C9]/80 bg-[#F7F0E6]/80 pl-10 pr-10 py-3 text-sm font-bold text-[#786B62] cursor-not-allowed select-none"
              />
              <span class="absolute inset-y-0 right-0 pr-3.5 flex items-center pointer-events-none text-[#786B62]/60">
                <i class="mdi mdi-lock text-sm"></i>
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Action Button -->
      <div class="border-t border-[#E8D9C9] pt-6 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <p class="text-xs text-[#786B62] font-semibold flex items-center gap-1">
          <i class="mdi mdi-information-outline text-[#D96C2C]"></i>
          <span>กดบันทึกเพื่ออัปเดตข้อมูลของคุณเข้าสู่ระบบทันที</span>
        </p>

        <button
          type="submit"
          class="inline-flex items-center justify-center gap-2 px-8 py-3 rounded-full bg-gradient-to-r from-[#D96C2C] to-[#BF5720] text-white font-black text-sm hover:-translate-y-0.5 shadow-lg shadow-[#D96C2C]/25 transition-all duration-200 active:scale-95 disabled:opacity-50 cursor-pointer border border-[#D96C2C]"
          :disabled="props.saving"
        >
          <i
            class="mdi text-lg text-white"
            :class="props.saving ? 'mdi-loading animate-spin' : 'mdi-content-save-check'"
          ></i>
          <span class="!text-white font-black">{{ props.saving ? 'กำลังบันทึกข้อมูล...' : 'บันทึกการเปลี่ยนแปลง' }}</span>
        </button>
      </div>
    </form>
  </div>
</template>