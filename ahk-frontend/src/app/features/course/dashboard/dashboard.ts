import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CourseProbeClient, CourseNoteDto } from '../../../api/api-client';
import { CourseContextService } from '../../../core/course/course-context.service';

/**
 * Course landing page. For this skeleton it reads/writes the course-scoped probe notes to demonstrate that
 * course data is confined to the active course. Real course dashboards (submissions, grades) replace it.
 */
@Component({
  selector: 'app-course-dashboard',
  imports: [FormsModule, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class CourseDashboard implements OnInit {
  private readonly client = inject(CourseProbeClient);
  protected readonly courseContext = inject(CourseContextService);

  protected readonly notes = signal<CourseNoteDto[]>([]);
  protected text = '';

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    const slug = this.courseContext.activeSlug();
    if (!slug) {
      return;
    }
    this.client.getNotes(slug).subscribe((list) => this.notes.set(list));
  }

  protected add(): void {
    const slug = this.courseContext.activeSlug();
    if (!slug || !this.text.trim()) {
      return;
    }
    this.client.addNote({ text: this.text }, slug).subscribe(() => {
      this.text = '';
      this.reload();
    });
  }
}
