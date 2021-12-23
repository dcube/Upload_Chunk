import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Guid } from 'guid-typescript';
import { throwError, Observable, forkJoin, Subscription } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { Result } from './result';

@Injectable({
    providedIn: 'root'
  })
  export class VideoService {
    private baseUrl = `http://toto/video`;
    private chunkSize = 1048576;
    private filesChunk = new Subscription();
  
    constructor(private httpClient: HttpClient) {}

    uploadFile(file: File, yieldResultFunc: (result: Result | null, total: number, error: any) => void) {
        const id = Guid.create().toString();
        const reader = new FileReader();
        const fileName = file.name;
    
        reader.onload = (e: ProgressEvent) =>
          // after reading, start sending data
          this.sendBufferData(reader.result, id, fileName, yieldResultFunc);
    
        // read file data as ArrayBuffer
        reader.readAsArrayBuffer(file);
      }

      private async sendBufferData(
        readResult: string | ArrayBuffer | null,
        id: string,
        fileName: string,
        yieldResultFunc: (result: Result | null, total: number, error: any) => void
      ) {
        let buffer = new Uint8Array(readResult as ArrayBuffer);
        this.filesChunk = forkJoin(this.getChunkRequests(buffer, id, yieldResultFunc)).subscribe((result) => {
          let hasFailedChunkFiles: Result | undefined = result.find((res: Result) => res.isSuccess === false);
    
          if (!hasFailedChunkFiles) {
            const url = `${this.baseUrl}/files/finalize?id=${id}&fileName=${fileName}`;
            const params = new HttpParams({});
    
            this.httpClient
              .post<Result>(url, { params })
              .pipe(
                catchError((err) => {
                  err.message = `Erreur lors de la finalisation de l'upload du fichier ${id}`;
                  return throwError(err);
                })
              )
              .subscribe();
          }
        });
      }

      private getChunkRequests(buffer: Uint8Array, id: string, yieldResultFunc: (result: Result | null, total: number, error: any) => void) {
        const url = `${this.baseUrl}/files/chunk`;
        let fileIndex = 0;
        const requestArray = new Array<Observable<Result>>();
        const total = Math.round(buffer.length / this.chunkSize);
        for (let i = 0; i < buffer.length; i += this.chunkSize) {
          let indexTo = i + this.chunkSize;
          if (indexTo >= buffer.length) {
            indexTo = buffer.length - 1; // for last data.
          }
          const formData = new FormData();
          formData.append('file', new Blob([buffer.subarray(i, indexTo)]));
          const params = new HttpParams({
            fromString: `id=${id}&index=${fileIndex.toString()}`
          });
          const request = this.httpClient
            .post<Result>(url, formData, { params })
            .pipe(
              tap(
                (result) => {
                  return yieldResultFunc(result, total, null);
                },
                (error) => {
                  return yieldResultFunc(null, total, error);
                }
              )
            );
          requestArray.push(request);
          fileIndex += 1;
        }
        return requestArray;
      }
    
      stopUploadVideo() {
        this.filesChunk.unsubscribe();
      }
  }