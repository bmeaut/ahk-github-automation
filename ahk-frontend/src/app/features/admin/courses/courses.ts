import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CoursesAdminClient, CourseDto, CreateCourseRequest } from '../../../api/api-client';

/** Host/admin screen: list and create courses (their connected GitHub environments come in the port). */
@Component({
  selector: 'app-admin-courses',
  imports: [FormsModule, DatePipe],
  templateUrl: './courses.html',
  styleUrl: './courses.scss',
})
export class AdminCourses implements OnInit {
  private readonly client = inject(CoursesAdminClient);

  protected readonly courses = signal<CourseDto[]>([]);
  protected readonly error = signal<string | null>(null);

  protected slug = '';
  protected name = '';

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.client.list().subscribe({
      next: (list) => this.courses.set(list),
      error: () => this.error.set('Failed to load courses.'),
    });
  }

  protected create(): void {
    this.error.set(null);
    const request: CreateCourseRequest = { slug: this.slug, name: this.name };
    this.client.create(request).subscribe({
      next: () => {
        this.slug = '';
        this.name = '';
        this.reload();
      },
      error: () => this.error.set('Failed to create course (slug may already exist).'),
    });
  }
}
