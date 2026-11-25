<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, RouterLink } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { getApiErrorMessage } from '../api/getApiErrorMessage'
import { useSwal } from '@/plugins/sweetalert'
import heroCoverImage from '@/assets/รูปปก.png'

const email = ref('')
const password = ref('')
const showPassword = ref(false)
const rememberMe = ref(false)
const loading = ref(false)
const router = useRouter()
const auth = useAuthStore()
const swal = useSwal()

async function submit() {
  if (!email.value.trim() || !password.value) {
    await swal.warning('กรอกข้อมูลไม่ครบ', 'กรุณากรอกอีเมลและรหัสผ่านให้ครบถ้วน')
    return
  }

  loading.value = true
  try {
    await auth.signIn(email.value.trim(), password.value)
    await swal.success('เข้าสู่ระบบสำเร็จ', 'ยินดีต้อนรับเข้าสู่ระบบ กาญจน์ชวนดู')
    await router.replace(auth.user?.role === 'Admin' ? '/admin' : '/')
  } catch (error) {
    await swal.error('เข้าสู่ระบบไม่สำเร็จ', getApiErrorMessage(error, 'อีเมลหรือรหัสผ่านไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง'))
  } finally {
    loading.value = false
  }
}

function handleForgotPassword() {
  void swal.info(
    'ลืมรหัสผ่าน',
    'กรุณาติดต่อผู้ดูแลระบบเพื่อขอรีเซ็ตรหัสผ่านของคุณ',
  )
}
</script>

<template>
  <main
    class="relative flex min-h-[calc(100dvh-4rem)] items-center justify-center overflow-hidden bg-[#F7F0E6] p-4 sm:min-h-[calc(100dvh-5rem)] sm:p-6 lg:p-8"
  >
    <!-- Soft brand glows -->
    <div class="pointer-events-none absolute -top-24 -right-16 h-80 w-80 rounded-full bg-[#F2A65A]/25 blur-3xl"></div>
    <div class="pointer-events-none absolute -bottom-28 -left-10 h-72 w-72 rounded-full bg-[#D96C2C]/15 blur-3xl"></div>

    <!-- Split card (desktop: 2 columns / mobile: form only) -->
    <div
      class="relative grid min-h-[580px] w-full max-w-4xl grid-cols-1 overflow-hidden rounded-[1.75rem] border-2 border-[#E8D9C9] bg-white shadow-xl shadow-[#D96C2C]/10 md:grid-cols-12"
    >
      <!-- Left: visual showcase & branding -->
      <div
        class="relative hidden flex-col justify-between overflow-hidden bg-[#2C221E] p-8 text-white md:col-span-5 md:flex"
      >
        <img
          :src="heroCoverImage"
          alt="เมืองกาญจนบุรี"
          class="absolute inset-0 h-full w-full scale-105 object-cover object-center opacity-40"
        />
        <div class="absolute inset-0 bg-gradient-to-t from-[#171412] via-[#3A2A20]/75 to-[#2C221E]/80"></div>
        <div class="absolute -top-20 -right-20 h-64 w-64 rounded-full bg-[#D96C2C]/30 blur-3xl"></div>

        <div class="relative z-10 space-y-3">
          <div
            class="inline-flex items-center gap-2 rounded-full border border-white/20 bg-white/10 px-3 py-1 text-xs font-bold text-[#F2A65A] backdrop-blur-md"
          >
            <span class="h-2 w-2 rounded-full bg-[#F2A65A]"></span>
            <span>กาญจน์ชวนดู</span>
          </div>
          <h2 class="text-2xl leading-snug font-black tracking-tight">
            เที่ยว ชิม ช้อป<br />
            <span class="text-[#F2A65A]">สัมผัสเสน่ห์เมืองกาญจน์</span>
          </h2>
        </div>

        <div class="relative z-10 my-6 space-y-3">
          <div class="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/10 p-3 backdrop-blur-md">
            <div
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-[#F2A65A]/30 bg-[#D96C2C]/30 text-[#F2A65A]"
            >
              <i class="mdi mdi-storefront text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">รวมสินค้าและร้านค้าชุมชน</p>
              <p class="text-[11px] font-medium text-[#F7F0E6]/75">ของดีเมืองกาญจน์จากผู้ประกอบการท้องถิ่น</p>
            </div>
          </div>

          <div class="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/10 p-3 backdrop-blur-md">
            <div
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-[#F2A65A]/30 bg-[#D96C2C]/30 text-[#F2A65A]"
            >
              <i class="mdi mdi-map-marker-radius text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">เรื่องราวท่องเที่ยวและกิจกรรม</p>
              <p class="text-[11px] font-medium text-[#F7F0E6]/75">ค้นพบสถานที่ จุดเช็กอิน วัฒนธรรมน่าสนใจ</p>
            </div>
          </div>

          <div class="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/10 p-3 backdrop-blur-md">
            <div
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-[#F2A65A]/30 bg-[#D96C2C]/30 text-[#F2A65A]"
            >
              <i class="mdi mdi-shield-check-outline text-lg"></i>
            </div>
            <div>
              <p class="text-xs font-bold text-white">ระบบจัดการมาตรฐาน ปลอดภัย</p>
              <p class="text-[11px] font-medium text-[#F7F0E6]/75">ใช้งานง่าย ตรวจสอบออเดอร์ได้ตลอดเวลา</p>
            </div>
          </div>
        </div>

        <div
          class="relative z-10 flex items-center justify-between border-t border-white/10 pt-3 text-[11px] font-medium text-[#F7F0E6]/60"
        >
          <span>Kanchanaburi Platform</span>
          <span>© 2026 สงวนลิขสิทธิ์</span>
        </div>
      </div>

      <!-- Right: login form -->
      <div class="flex flex-col justify-between space-y-6 p-6 sm:p-10 md:col-span-7">
        <div>
          <div class="mb-6 flex items-center justify-between gap-3">
            <RouterLink
              to="/"
              class="inline-flex cursor-pointer items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-3 py-1.5 text-xs font-bold text-[#332820] transition select-none hover:border-[#D96C2C] hover:text-[#D96C2C] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/20 focus-visible:outline-none"
            >
              <i class="mdi mdi-arrow-left text-sm text-[#D96C2C]"></i>
              <span>กลับสู่หน้าหลัก</span>
            </RouterLink>

            <span
              class="inline-flex items-center gap-1.5 rounded-full border border-[#E8D9C9] bg-[#FFF9F2] px-3 py-1.5 text-xs font-bold text-[#D96C2C]"
            >
              <span class="h-2 w-2 rounded-full bg-[#D96C2C]"></span>
              <span>เข้าสู่ระบบ</span>
            </span>
          </div>

          <div class="mb-6 space-y-1.5">
            <h1 class="text-2xl font-black tracking-tight text-[#332820] sm:text-3xl">
              เข้าสู่ระบบ<span class="text-[#D96C2C]">สมาชิก</span>
            </h1>
            <p class="text-xs font-semibold text-[#786B62] sm:text-sm">
              กรอกอีเมลและรหัสผ่านเพื่อเข้าสู่บัญชี กาญจน์ชวนดู
            </p>
          </div>

          <form class="space-y-4" @submit.prevent="submit">
            <!-- Email -->
            <div class="space-y-1.5">
              <label for="login-email" class="block text-xs font-bold text-[#332820]">
                อีเมลผู้ใช้งาน <span class="text-[#D96C2C]">*</span>
              </label>
              <div class="group relative flex items-center">
                <i
                  class="mdi mdi-email-outline absolute left-4 text-lg text-[#A8988C] transition group-focus-within:text-[#D96C2C]"
                ></i>
                <input
                  id="login-email"
                  v-model="email"
                  type="email"
                  autocomplete="email"
                  required
                  placeholder="name@example.com"
                  class="w-full rounded-2xl! border-2 border-[#E8D9C9] bg-[#FFF9F2] py-3 pr-4 pl-11 text-sm font-bold text-[#332820] shadow-2xs transition placeholder:font-medium placeholder:text-[#A8988C] focus:border-[#D96C2C] focus:bg-white focus:ring-4 focus:ring-[#D96C2C]/15 focus:outline-none"
                />
              </div>
            </div>

            <!-- Password -->
            <div class="space-y-1.5">
              <label for="login-password" class="block text-xs font-bold text-[#332820]">
                รหัสผ่าน <span class="text-[#D96C2C]">*</span>
              </label>
              <div class="group relative flex items-center">
                <i
                  class="mdi mdi-lock-outline absolute left-4 text-lg text-[#A8988C] transition group-focus-within:text-[#D96C2C]"
                ></i>
                <input
                  id="login-password"
                  v-model="password"
                  :type="showPassword ? 'text' : 'password'"
                  autocomplete="current-password"
                  required
                  placeholder="กรอกรหัสผ่านของคุณ"
                  class="w-full rounded-2xl! border-2 border-[#E8D9C9] bg-[#FFF9F2] py-3 pr-11 pl-11 text-sm font-bold text-[#332820] shadow-2xs transition placeholder:font-medium placeholder:text-[#A8988C] focus:border-[#D96C2C] focus:bg-white focus:ring-4 focus:ring-[#D96C2C]/15 focus:outline-none"
                />
                <button
                  type="button"
                  class="absolute right-3 flex h-8 w-8 cursor-pointer items-center justify-center rounded-full text-[#786B62] transition select-none hover:bg-[#F7F0E6] hover:text-[#332820]"
                  :title="showPassword ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน'"
                  :aria-label="showPassword ? 'ซ่อนรหัสผ่าน' : 'แสดงรหัสผ่าน'"
                  @click="showPassword = !showPassword"
                >
                  <i class="mdi text-lg" :class="showPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"></i>
                </button>
              </div>
            </div>

            <!-- Remember me & forgot password -->
            <div class="flex items-center justify-between pt-1 text-xs">
              <label class="flex cursor-pointer items-center gap-2 font-bold text-[#332820] select-none">
                <input
                  v-model="rememberMe"
                  type="checkbox"
                  class="h-4 w-4 cursor-pointer rounded-md border-2 border-[#E8D9C9] accent-[#D96C2C]"
                />
                <span>จดจำฉันไว้ในระบบ</span>
              </label>

              <button
                type="button"
                class="cursor-pointer font-black text-[#D96C2C]! select-none hover:text-[#BF5720]! hover:underline"
                @click="handleForgotPassword"
              >
                ลืมรหัสผ่าน?
              </button>
            </div>

            <!-- Submit -->
            <div class="pt-2">
              <button
                type="submit"
                class="inline-flex w-full cursor-pointer items-center justify-center gap-2 rounded-full bg-[#D96C2C] px-6 py-3.5 text-sm font-black text-white shadow-lg shadow-[#D96C2C]/30 transition hover:bg-[#BF5720] focus-visible:ring-4 focus-visible:ring-[#D96C2C]/30 focus-visible:outline-none active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-70"
                :disabled="loading"
              >
                <i
                  class="mdi text-lg text-white"
                  :class="loading ? 'mdi-loading mdi-spin' : 'mdi-login-variant'"
                />
                <span>{{ loading ? 'กำลังเข้าสู่ระบบ...' : 'เข้าสู่ระบบ' }}</span>
              </button>
            </div>
          </form>
        </div>

        <!-- Switch to register -->
        <div class="space-y-2 border-t-2 border-[#E8D9C9] pt-5 text-center">
          <p class="text-xs font-semibold text-[#786B62]">
            ยังไม่มีบัญชีสมาชิกสำหรับใช้งาน?
            <RouterLink
              to="/register"
              class="ml-1 font-black text-[#D96C2C] hover:text-[#BF5720] hover:underline"
            >
              สมัครสมาชิกใหม่
            </RouterLink>
          </p>

          <p class="text-[11px] font-medium text-[#A8988C]">
            ระบบความปลอดภัยมาตรฐาน ข้อมูลส่วนตัวของคุณจะถูกปกป้องอย่างเคร่งครัด
          </p>
        </div>
      </div>
    </div>
  </main>
</template>
