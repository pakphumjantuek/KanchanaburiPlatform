import http from '@/shared/api/http'
import type { PagedResult } from '@/shared/interface/PagedResult'

export interface Tag {
  tagId: string
  tagName: string
  status: 'Active' | 'Inactive'
}

export async function getTags(params: {
  status?: string | null
  search?: string | null
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
  const { data } = await http.get<PagedResult<Tag>>('/tags/admin', { params: queryParams })
  if (params.search && params.search.trim()) {
    const q = params.search.toLowerCase().trim()
    const filtered = data.items.filter((t) => t.tagName.toLowerCase().includes(q))
    return {
      ...data,
      items: filtered,
      totalCount: filtered.length,
    }
  }
  return data
}

export async function getTag(id: string) {
  const { data } = await http.get<Tag>(`/tags/admin/${id}`)
  return data
}

export async function createTag(data: { tagName: string }) {
  const { data: result } = await http.post<Tag>('/tags', data)
  return result
}

export async function updateTag(id: string, data: { tagName: string; status: 'Active' | 'Inactive' }) {
  await http.put(`/tags/${id}`, data)
}

export async function deleteTag(id: string) {
  await http.delete(`/tags/${id}`)
}

export async function setTagStatus(id: string, tag: Tag, status: 'Active' | 'Inactive') {
  if (status === 'Inactive') {
    await http.delete(`/tags/${id}`)
  } else {
    await http.put(`/tags/${id}`, {
      tagName: tag.tagName,
      status: 'Active',
    })
  }
}
