import { Component, OnInit } from '@angular/core';
import { FileInput } from 'ngx-material-file-input';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Guid } from 'guid-typescript';
import { VideoService } from 'src/app/core/video.service';
import { Result } from 'src/app/core/result';
import { Subscription} from 'rxjs';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit {
  form!: FormGroup;
  uploadFile?: File;
  isLoading: boolean = false;
  finishUploadVideo: boolean = false;
  uploadProgress = 0;
  chunkRequestApi = new Subscription();

  constructor(private formBuilder: FormBuilder, private videoService: VideoService) { }


  ngOnInit(): void {

    this.form = this.formBuilder.group({
      video: [undefined, Validators.required]
    })

    this.form.controls['video'].valueChanges.subscribe((fileInput: FileInput) => {
      if (fileInput !== null && fileInput.files.length > 0){
        this.uploadFile = fileInput.files[0];
      } else {
        this.uploadFile = undefined;
      }
    });
  }

  //Dans cette méthode, mettre tout ce qui concerne l'annulation de l'upload
  cancelUpload() {
    this.chunkRequestApi.unsubscribe()
    this.isLoading = false;
    this.uploadProgress = 0;
  }

  uploadVideo() {
    if (this.uploadFile === undefined) {
      return;
    }

    this.isLoading = true;

    const id = Guid.create();//Creation de l'identifiant du fichier
    let nbUploadeChunck = 1;//Nombre de chunck qui ont été envoyé
    let total = this.videoService.getTotalChunks(this.uploadFile!.size);//Nombre total de chunks à envoyer
    let resultList: Result[] = [];//On enregistre tous les résultats d'upload pour savoir s'il y a eu des erreurs
    this.chunkRequestApi = this.videoService.uploadFile(id, this.uploadFile!)
    .subscribe({
      next: (result) =>{
        this.uploadProgress = Math.round((nbUploadeChunck / total) * 100);
        resultList.push(result);

        //S'il y a une erreur, on arrete l'envoie
        if (result != null && result.isSuccess === false) {
          this.cancelUpload();

          if (result.message) {
            console.log(result.message);
          }
        }
        nbUploadeChunck++;
        if (nbUploadeChunck > total) {
          this.finishUploadVideo = true;
          console.log('La vidéo a été correctement ajouté');
          this.videoService.finalize(id, resultList, this.uploadFile!.name).subscribe({
            next: (result) => {
              console.log('Finalize terminé');
              this.isLoading = false;
            },
            error: (e) => console.log(e.message)
          })
        }
      },
      error: (e) => console.log(e.message)
    });
      
  }

}
