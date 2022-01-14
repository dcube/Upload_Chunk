import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Guid } from 'guid-typescript';
import { throwError, Observable, from, mergeAll, of } from 'rxjs';
import { tap, retryWhen, concatMap, delay } from 'rxjs/operators';
import { Result } from './result';


@Injectable({
    providedIn: 'root'
  })
  export class VideoService {
    private baseUrl = `http://localhost:7071/api`;
    private chunkSize = 5242880;//5Mo
  
    constructor(private httpClient: HttpClient) {}

    getTotalChunks(fileSize: number){
      return Math.ceil(fileSize / this.chunkSize);
    }

    uploadFile(id: Guid, file: File){
      const observables = this.getChunkRequests(id, file);

      return from(observables)
      .pipe(mergeAll(5));
    }

    finalize(id: Guid, resultList: Result[], fileName: string){
      let hasFailedChunkFiles: Result | undefined = resultList.find((res: Result) => res.isSuccess === false);
    
      if (!hasFailedChunkFiles) {
        const url = `${this.baseUrl}/files/finalize?fileid=${id.toString()}&fileName=${fileName}`;
        const params = new HttpParams({});

        return this.httpClient
          .post<Result>(url, { params });
      }
      return throwError(() => 'Certains chunks ont échoué');
    }

    private getChunkRequests(id: Guid, file: File) {
      console.log('getChunkRequests');

      const requestArray = new Array<Observable<Result>>();
      const url = `${this.baseUrl}/files/chunk`;

      // Number of parts (exclusive last part!)
      var chunks = this.getTotalChunks(file.size);
      // Iterate the parts
      for (var i = 0; i < chunks; i++) {
        var start = i * this.chunkSize;
        var end = Math.min(start + this.chunkSize, file.size);

        const formData = new FormData();
        formData.append('file', file.slice(start, end));

        const params = new HttpParams({
          fromString: `fileid=${id.toString()}&index=${i.toString()}`
        });
        const request = this.httpClient
          .post<Result>(url, formData, { params }).pipe(tap(
            (result)=>{
            return result;
          },
          (error) => {
            console.log(error);
            return error;
          }),
          retryWhen(errors => errors
            .pipe(
              concatMap((error, count) => {
                if (count < 5 && (error.status == 400 || error.status == 0)) {
                    return of(error.status);
                }
                console.log('retry : ' + error);
                return throwError(() =>error);
              }),
              delay(1000)
            )));

        requestArray.push(request);
      }

      return requestArray;
      
    }
  }