import { Component, OnInit } from '@angular/core';
import { FileInput } from 'ngx-material-file-input';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { VideoService } from 'src/app/core/video.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  form!: FormGroup;
  uploadFile!: File;
  isLoading: boolean = false;
  finishUploadVideo: boolean = false;
  uploadProgress = 0;

  constructor(private formBuilder: FormBuilder, private videoService: VideoService,) { }


  ngOnInit(): void {

    this.form = this.formBuilder.group({
      video: [undefined, Validators.required]
    })

    this.form.controls['video'].valueChanges.subscribe((fileInput: FileInput) => {
      this.uploadFile = fileInput.files[0];
    });
  }

  cancelUpload() {
    this.isLoading = false;
    this.uploadProgress = 0;
  }

  uploadVideo() {
    if (this.uploadFile == null) {
      return;
    }

    this.isLoading = true;

    let index = 1;
    this.videoService.uploadFile(this.uploadFile, (result, total, error) => {
      if (error) {
        console.log(error.message);
      }

      this.uploadProgress = Math.round((index / total) * 100);

      if (result != null && result.isSuccess === false) {
        this.cancelUpload();

        if (result.message) {
          console.log(result.message);
        }
      }

      index++;

      if (index > total) {
        this.finishUploadVideo = true;
        console.log('La vidéo a été correctement ajouté');
      }
    });
  }

}
