import { Component, EventEmitter, Input, Output } from '@angular/core';
import type * as monaco from 'monaco-editor';

@Component({
  selector: 'app-coding',
  standalone: false,
  templateUrl: './coding.component.html',
  styleUrl: './coding.component.scss'
})
export class CodingComponent {
  @Input() title = 'Code Editor';
  @Input() description = 'Use the shared coding workspace to draft, review, and refine code.';
  @Input() helperText = 'Choose a language and keep your work in sync through a single reusable editor.';
  @Input() language = 'javascript';
  @Input() code = '';
  @Input() theme: 'vs' | 'vs-dark' | 'hc-black' = 'vs';
  @Input() readOnly = false;
  @Input() height = '420px';
  @Input() editorOptions: monaco.editor.IStandaloneEditorConstructionOptions = {};

  @Output() codeChange = new EventEmitter<string>();

  onCodeChange(value: string): void {
    this.code = value;
    this.codeChange.emit(value);
  }
}
