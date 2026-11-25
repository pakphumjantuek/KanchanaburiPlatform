import http from '@/shared/api/http'
import type { Shop } from '@/features/shops/shared/types/shop'
import type { PagedResult } from '@/shared/interface/PagedResult'

export async function getAdminShops(params: {
  search?: string
  status?: string | null
  page: number
  pageSize: number
}) {
  const queryParams: Record<string, unknown> = {
    page: params.page,
    pageSize: params.pageSize,
  }
  if (params.status && params.status !== 'All') {
    queryParams.status = params.status
  }
  if (params.search && params.search.trim()) {
    queryParams.search = params.search.trim()
  }
  const { data } = await http.get<PagedResult<Shop>>('/shops/admin', { params: queryParams })
  return data
}

export async function getAdminShop(id: string) {
  const { data } = await http.get<Shop>(`/shops/admin/${id}`)
  return data
}

export async function updateShopStatus(id: string, status: string) {
  await http.patch(`/shops/${id}/status`, { status })
}
