import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';

/** Dữ liệu địa giới rút gọn: n = tên, d = quận/huyện, w = phường/xã. Nguồn: provinces.open-api.vn. */
export interface AddressDistrict { n: string; w: string[]; }
export interface AddressProvince { n: string; d: AddressDistrict[]; }

/** Tải danh sách tỉnh / quận / phường một lần (file tĩnh ~220KB trong public/data), các lần sau dùng cache. */
@Injectable({ providedIn: 'root' })
export class AddressService {
  private http = inject(HttpClient);
  private data$?: Observable<AddressProvince[]>;

  load(): Observable<AddressProvince[]> {
    return this.data$ ??= this.http.get<AddressProvince[]>('/data/vn-address.json').pipe(shareReplay(1));
  }
}
