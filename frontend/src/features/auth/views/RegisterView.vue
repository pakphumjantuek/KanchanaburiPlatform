<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { register } from '../api/authApi'
import { getApiErrorMessage } from '../api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import heroCoverImage from '@/assets/รูปปก.png'

const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const showPassword = ref(false)
const showConfirmPassword = ref(false)
const acceptTerms = ref(true)
const loading = ref(false)
const router = useRouter()
const swal = useSwal()

async function submit() {
  if (!email.value.trim() || !password.value || !confirmPassword.value) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณากรอกอีเมล รหัสผ่าน และยืนยันรหัสผ่านให้ครบถ้วน')
    return
  }
  if (password.value.length < 6) {
    await swal.warning('รหัสผ่านสั้นเกินไป', 'รหัสผ่านต้องมีความยาวอย่างน้อย 6 ตัวอักษร')
    return
  }
  if (password.value !== confirmPassword.value) {
    await swal.warning('รหัสผ่านไม่ตรงกัน', 'กรุณาตรวจสอบและยืนยันรหัสผ่านใหม่อีกครั้ง')
    return
  }
  if (!acceptTerms.value) {
    await swal.warning('ข้อตกลงการใช้งาน', 'กรุณายอมรับเงื่อนไขและข้อตกลงการใช้งาน')
    return
  }

  loading.value = true
  try {
    await register(email.value.trim(), password.value)
    await swal.success('สมัครสมาชิกสำเร็จ', 'ยินดีต้อนรับสู่ กาญจน์ชวนดู กรุณาเข้าสู่ระบบเพื่อเริ่มใช้งาน')
    await router.replace('/login')
  } catch (error) {
    await swal.error('สมัครสมาชิกไม่สำเร็จ', getApiErrorMessage(error, 'ไม่สามารถสมัครสมาชิกได้ กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="min-h-[calc(100dvh-4rem)] sm:min-h-[calc(100dvh-5rem)] bg-[#F7F0E6] flex items-center justify-center p-4 sm:p-6 lg:p-8 font-sans">
    <!-- Centered Split Container -->
    <div class="w-full max-w-4xl overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xl grid grid-cols-1 md:grid-cols-12 min-h-[620px]">
      
      <!-- Left Column: Kanchanaburi Visual Showcase & Branding -->
      <div class="relative md:col-span-5 hidden md:flex flex-col justify-between p-8 text-white overflow-hidden bg-[#2C221E]">
        <!-- Background Hero Image with Gradient -->
        <img
          :src="heroCoverImage"
          alt="เมืองกาญจนบุรี"
          class="absolute inset-0 h-full w-full object-cover object-center opacity-40 scale-105"
        />
        <div class="absolute inset-0 bg-gradient-to-t from-[#171412] via-[#3A2A20]/75 to-[#2C221E]/80"></div>

        <!-- Top Branding -->
        <div class="relative z-10 space-y-2">
          <div class="inline-flex items-center gap-2 rounded-full bg-white/10 backdrop-blur-md px-3 py-1 text-xs font-bold text-[#F2A65A] border border-white/20">
            <span class="h-2 w-2 rounded-full bg-[#F2A65A]"></span>
            <span>ร่วมเป็นส่วนหนึ่งกับเรา</span>
          </div>
          <h2 class="text-2xl font-black tracking-tight leading-snug">
            สร้างบัญชีสมาชิก<br />กาญจน์ชวนดู วันนี้
          </h2>
        </div>

        <!-- Middle Feature Highlights -->
        <div class="relative z-10 space-y-3 my-6">
          <div class="flex items-center gap-3 rounded-2xl bg-white/10 backdrop-blur-md p-3 border border-white/10">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#FFF9F2]0/20 text-[#F2A65A] border border-[#F2A65A]/30">
              <i class="mdi mdi-cart-arrow-down text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">ช้อปสินค้าท้องถิ่นสะดวก</p>
              <p class="text-[11px] text-[#F7F0E6]/75 font-medium">สั่งซื้อสินค้าชุมชนและรับโปรโมชันพิเศษ</p>
            </div>
          </div>

          <div class="flex items-center gap-3 rounded-2xl bg-white/10 backdrop-blur-md p-3 border border-white/10">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#D96C2C]/30 text-[#F2A65A] border border-[#F2A65A]/30">
              <i class="mdi mdi-store-plus text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">เปิดร้านค้าออนไลน์ของตนเอง</p>
              <p class="text-[11px] text-[#F7F0E6]/75 font-medium">สร้างร้านค้าและลงขายสินค้าได้ทันที</p>
            </div>
          </div>

          <div class="flex items-center gap-3 rounded-2xl bg-white/10 backdrop-blur-md p-3 border border-white/10">
            <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-[#FFF9F2]0/20 text-[#F2A65A] border border-[#F2A65A]/30">
              <i class="mdi mdi-pencil-outline text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">เขียนบอกเล่าเรื่องราวเมืองกาญจน์</p>
              <p class="text-[11px] text-[#F7F0E6]/75 font-medium">แชร์สถานที่น่าเที่ยวและบทความชุมชน</p>
            </div>
          </div>
        </div>

        <!-- Bottom Footer -->
        <div class="relative z-10 text-[11px] text-[#F7F0E6]/60 font-medium flex items-center justify-between border-t border-white/10 pt-3">
          <span>Kanchanaburi Platform</span>
          <span>© 2026 สงวนลิขสิทธิ์</span>
        </div>
      </div>

      <!-- Right Column: Register Form -->
      <div class="md:col-span-7 p-6 sm:p-10 flex flex-col justify-between space-y-6">
        <div>
          <!-- Back to Home Link -->
          <div class="flex items-center justify-between mb-4">
            <RouterLink
              to="/"
              class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 bg-[#FFF9F2] hover:bg-[#F7F0E6] text-xs font-bold text-[#332820] transition cursor-pointer select-none"
            >
              <i class="mdi mdi-arrow-left text-sm text-[#786B62]"></i>
              <span>กลับสู่หน้าหลัก</span>
            </RouterLink>

            <span class="inline-flex items-center gap-1.5 rounded-full bg-[#FFF9F2] border border-[#E8D9C9] px-3 py-1 text-xs font-bold text-[#D96C2C]">
              <span class="h-2 w-2 rounded-full bg-[#D96C2C]"></span>
              <span>สมัครสมาชิกใหม่</span>
            </span>
          </div>

          <!-- Title Header -->
          <div class="space-y-1 mb-5">
            <h1 class="text-2xl sm:text-3xl font-black text-[#332820] tracking-tight">
              สมัครสมาชิกใหม่
            </h1>
            <p class="text-xs sm:text-sm font-semibold text-[#786B62]">
              เริ่มต้นสัมผัสเสน่ห์เมืองกาญจน์ได้ง่ายๆ เพียงกรอกข้อมูลด้านล่าง
            </p>
          </div>

          <!-- Form Fields -->
          <form class="space-y-3.5" @submit.prevent="submit">
            <!-- Email Input -->
            <div class="space-y-1">
              <label class="block text-xs font-bold text-[#332820]">
                อีเมลของคุณ *
              </label>
              <div class="relative flex items-center">
                <i class="mdi mdi-email-outline absolute left-4 text-lg text-[#A8988C]"></i>
                <input
                  v-model="email"
                  type="email"
                  autocomplete="email"
                  required
                  placeholder="name@example.com"
                  class="w-full rounded-2xl! border-2 border-[#E8D9C9] bg-white py-2.5 pl-11 pr-4 text-xs sm:text-sm font-bold text-[#332820] placeholder:text-[#A8988C] focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/15 focus:outline-none transition shadow-2xs"
                />
              </div>
            </div>

            <!-- Password Input -->
            <div class="space-y-1">
              <label class="block text-xs font-bold text-[#332820]">
                รหัสผ่าน * (อย่างน้อย 6 ตัวอักษร)
              </label>
              <div class="relative flex items-center">
                <i class="mdi mdi-lock-outline absolute left-4 text-lg text-[#A8988C]"></i>
                <input
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="new-password"
                  required
                  placeholder="กำหนดรหัสผ่านของคุณ"
                  class="w-full rounded-2xl! border-2 border-[#E8D9C9] bg-white py-2.5 pl-11 pr-11 text-xs sm:text-sm font-bold text-[#332820] placeholder:text-[#A8988C] focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/15 focus:outline-none transition shadow-2xs"
                />
                <button
                  type="button"
                  class="absolute right-3.5 flex h-7 w-7 items-center justify-center rounded-full text-[#A8988C] hover:text-[#332820] cursor-pointer select-none transition"
                  @click="showPassword = !showPassword"
                  title="แสดง/ซ่อนรหัสผ่าน"
                >
                  <i class="mdi text-lg" :class="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"></i>
                </button>
              </div>
            </div>

            <!-- Confirm Password Input -->
            <div class="space-y-1">
              <label class="block text-xs font-bold text-[#332820]">
                ยืนยันรหัสผ่านอีกครั้ง *
              </label>
              <div class="relative flex items-center">
                <i class="mdi mdi-lock-check-outline absolute left-4 text-lg text-[#A8988C]"></i>
                <input
                  v-model="confirmPassword"
                  :type="showConfirmPassword ? 'text' : 'password'"
                  autocomplete="new-password"
                  required
                  placeholder="กรอกรหัสผ่านซ้ำอีกครั้ง"
                  class="w-full rounded-2xl! border-2 border-[#E8D9C9] bg-white py-2.5 pl-11 pr-11 text-xs sm:text-sm font-bold text-[#332820] placeholder:text-[#A8988C] focus:border-[#D96C2C] focus:ring-4 focus:ring-[#D96C2C]/15 focus:outline-none transition shadow-2xs"
                />
                <button
                  type="button"
                  class="absolute right-3.5 flex h-7 w-7 items-center justify-center rounded-full text-[#A8988C] hover:text-[#332820] cursor-pointer select-none transition"
                  @click="showConfirmPassword = !showConfirmPassword"
                  title="แสดง/ซ่อนรหัสผ่าน"
                >
                  <i class="mdi text-lg" :class="showConfirmPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"></i>
                </button>
              </div>
            </div>

            <!-- Terms Checkbox -->
            <div class="pt-1">
              <label class="flex items-start gap-2 cursor-pointer select-none text-xs font-semibold text-[#786B62]">
                <input
                  v-model="acceptTerms"
                  type="checkbox"
                  class="h-4 w-4 rounded-md border-2 border-[#E8D9C9] accent-[#D96C2C] focus:ring-0 cursor-pointer mt-0.5"
                />
                <span>ฉันยอมรับข้อตกลง เงื่อนไขการใช้งาน และนโยบายความเป็นส่วนตัวของแพลตฟอร์ม</span>
              </label>
            </div>

            <!-- Submit Button (Full Capsule Pill) -->
            <div class="pt-2">
              <button
                type="submit"
                class="w-full inline-flex items-center justify-center gap-2 rounded-full bg-[#D96C2C] hover:bg-[#BF5720] px-6 py-3.5 font-black text-sm text-white shadow-md transition active:scale-95 disabled:opacity-60 disabled:cursor-not-allowed cursor-pointer select-none outline-none focus:outline-none"
                :disabled="loading"
              >
                <i
                  class="mdi text-lg text-white"
                  :class="loading ? 'mdi-loading animate-spin' : 'mdi-account-plus-outline'"
                />
                <span>{{ loading ? 'กำลังสร้างบัญชี...' : 'สมัครสมาชิก' }}</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Footer: Switch to Login -->
        <div class="border-t-2 border-[#E8D9C9] pt-4 text-center space-y-1.5">
          <p class="text-xs font-semibold text-[#786B62]">
            มีบัญชีสมาชิกอยู่แล้ว?
            <RouterLink
              to="/login"
              class="font-black text-[#D96C2C] hover:text-[#BF5720] hover:underline ml-1"
            >
              เข้าสู่ระบบที่นี่
            </RouterLink>
          </p>

          <p class="text-[11px] text-[#A8988C] font-medium">
            ข้อมูลของคุณจะถูกเก็บรักษาอย่างปลอดภัยตามมาตรฐานความปลอดภัยสากล
          </p>
        </div>
      </div>
    </div>
  </main>
</template>
