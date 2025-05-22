import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
    albums: any[] = [];
    designs: any[] = [];
    selectedAlbum: string | null = null;

    constructor(private http: HttpClient) { }

    ngOnInit() {
        this.fetchAlbums();
    }

    fetchAlbums() {
        this.http.get<any[]>('http://localhost:5000/albums').subscribe(data => {
            this.albums = data;
        });
    }

    fetchDesigns(albumID: number) {
        this.selectedAlbum = this.albums.find(a => a.AlbumID === albumID)?.Caption || null;
        this.http.get<any[]>(`http://localhost:5000/albums/${albumID}/designs`).subscribe(data => {
            this.designs = data;
        });
    }
}
