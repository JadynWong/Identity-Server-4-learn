import { TodoService } from './../../services/todo.service';
import { Component, OnInit, ViewChild } from '@angular/core';
import { ITodo } from 'src/app/models/todo';

@Component({
  selector: 'ac-todo-table',
  templateUrl: './todo-table.component.html',
  styleUrls: ['./todo-table.component.scss'],
})
export class TodoTableComponent implements OnInit {
  todos: ITodo[];

  constructor(private todService: TodoService) {}

  /** Columns displayed in the table. Columns IDs can be added, removed, or reordered. */
  displayedColumns = ['id', 'name'];

  ngOnInit() {
    this.todService.getAllTodos().subscribe((todos) => {
      this.todos = todos;
      console.log(this.todos);
    });
  }
}
