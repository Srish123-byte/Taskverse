import {
  AfterViewInit,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  ViewChild
} from '@angular/core';
import type * as monaco from 'monaco-editor';

type MonacoNamespace = typeof monaco;
type MonacoAmdRequire = {
  (modules: string[], onLoad: () => void): void;
  config(configuration: { paths: Record<string, string> }): void;
};

declare global {
  interface Window {
    monaco?: MonacoNamespace;
    __monacoAmdRequire__?: MonacoAmdRequire;
  }
}

let monacoLoaderPromise: Promise<MonacoNamespace> | null = null;

@Component({
  selector: 'app-monaco-editor',
  standalone: false,
  templateUrl: './monaco-editor.component.html',
  styleUrl: './monaco-editor.component.scss'
})
export class MonacoEditorComponent implements AfterViewInit, OnChanges, OnDestroy {
  @ViewChild('editorHost', { static: true })
  private readonly editorHost?: ElementRef<HTMLDivElement>;

  @Input() value = '';
  @Input() language = 'javascript';
  @Input() theme: 'vs' | 'vs-dark' | 'hc-black' = 'vs';
  @Input() readOnly = false;
  @Input() height = '360px';
  @Input() options: monaco.editor.IStandaloneEditorConstructionOptions = {};

  @Output() valueChange = new EventEmitter<string>();

  private monacoApi: MonacoNamespace | null = null;
  private editorInstance: monaco.editor.IStandaloneCodeEditor | null = null;
  private resizeObserver: ResizeObserver | null = null;

  constructor(private readonly zone: NgZone) {}

  ngAfterViewInit(): void {
    void this.initializeEditor();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!this.editorInstance) {
      return;
    }

    if (changes['language'] && !changes['language'].firstChange) {
      const model = this.editorInstance.getModel();
      if (model && this.monacoApi) {
        this.monacoApi.editor.setModelLanguage(model, this.language);
      }
    }

    if (changes['theme'] && !changes['theme'].firstChange && this.monacoApi) {
      this.monacoApi.editor.setTheme(this.theme);
    }

    if (changes['value'] && !changes['value'].firstChange && this.value !== this.editorInstance.getValue()) {
      this.editorInstance.setValue(this.value);
    }

    if ((changes['readOnly'] && !changes['readOnly'].firstChange) || (changes['options'] && !changes['options'].firstChange)) {
      this.editorInstance.updateOptions(this.buildEditorOptions());
    }

    if (changes['height'] && !changes['height'].firstChange) {
      queueMicrotask(() => this.editorInstance?.layout());
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.editorInstance?.getModel()?.dispose();
    this.editorInstance?.dispose();
  }

  private async initializeEditor(): Promise<void> {
    this.monacoApi = await this.loadMonaco();
    this.createEditor();
  }

  private createEditor(): void {
    if (!this.editorHost || !this.monacoApi) {
      return;
    }

    const model = this.monacoApi.editor.createModel(this.value, this.language);
    this.editorInstance = this.monacoApi.editor.create(this.editorHost.nativeElement, {
      ...this.buildEditorOptions(),
      model
    });
    this.monacoApi.editor.setTheme(this.theme);

    this.zone.runOutsideAngular(() => {
      this.editorInstance?.onDidChangeModelContent(() => {
        const nextValue = this.editorInstance?.getValue() ?? '';
        if (nextValue === this.value) {
          return;
        }

        this.zone.run(() => {
          this.value = nextValue;
          this.valueChange.emit(nextValue);
        });
      });
    });

    if (typeof ResizeObserver !== 'undefined') {
      this.resizeObserver = new ResizeObserver(() => this.editorInstance?.layout());
      this.resizeObserver.observe(this.editorHost.nativeElement);
    }
  }

  private loadMonaco(): Promise<MonacoNamespace> {
    if (window.monaco) {
      return Promise.resolve(window.monaco);
    }

    if (monacoLoaderPromise) {
      return monacoLoaderPromise;
    }

    monacoLoaderPromise = new Promise<MonacoNamespace>((resolve, reject) => {
      const initializeLoader = (): void => {
        const amdRequire = window.__monacoAmdRequire__;
        if (!amdRequire) {
          reject(new Error('Monaco AMD loader did not initialize.'));
          return;
        }

        amdRequire.config({
          paths: {
            vs: 'assets/monaco/vs'
          }
        });

        amdRequire(['vs/editor/editor.main'], () => {
          if (!window.monaco) {
            reject(new Error('Monaco editor failed to load.'));
            return;
          }

          resolve(window.monaco);
        });
      };

      if (window.__monacoAmdRequire__) {
        initializeLoader();
        return;
      }

      const loaderScript = document.createElement('script');
      loaderScript.type = 'text/javascript';
      loaderScript.src = 'assets/monaco/vs/loader.js';
      loaderScript.onload = () => {
        window.__monacoAmdRequire__ = ((window as unknown) as { require?: MonacoAmdRequire }).require;
        initializeLoader();
      };
      loaderScript.onerror = () => reject(new Error('Unable to load the Monaco editor loader script.'));
      document.body.appendChild(loaderScript);
    });

    return monacoLoaderPromise;
  }

  private buildEditorOptions(): monaco.editor.IStandaloneEditorConstructionOptions {
    return {
      automaticLayout: true,
      fontSize: 14,
      fontLigatures: true,
      glyphMargin: false,
      minimap: { enabled: false },
      padding: { top: 16, bottom: 16 },
      readOnly: this.readOnly,
      roundedSelection: true,
      scrollBeyondLastLine: false,
      smoothScrolling: true,
      wordWrap: 'on',
      ...this.options
    };
  }
}
