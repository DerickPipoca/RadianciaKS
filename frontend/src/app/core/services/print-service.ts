import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class PrintService {
  printElement(elementId: string, title: string = 'Imprimir Documento'): void {
    const printWindow = window.open('', '_blank', 'width=300,height=600');

    if (printWindow) {
      const content = document.getElementById(elementId)?.innerHTML;

      if (!content) {
        console.error(`Elemento com ID ${elementId} não encontrado para impressão.`);
        return;
      }

      printWindow.document.write(`
        <html>
          <head>
            <title>${title}</title>
            <style>
              body { font-family: 'Courier New', monospace; font-size: 14px; margin: 0; padding: 10px; width: 80mm; }
              .receipt-container { width: 80mm; }
            </style>
          </head>
          <body>
            <div class="receipt-container">${content}</div>
          </body>
        </html>
      `);

      printWindow.document.close();
      printWindow.focus();

      setTimeout(() => {
        printWindow.print();
        printWindow.close();
      }, 100);
    }
  }
}
