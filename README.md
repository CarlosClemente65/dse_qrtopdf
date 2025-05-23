# QR_TO_PDF
## Programa para añadir un QR o una marca de agua a un PDF

### Desarrollado por Carlos Clemente (05-2025)

### Control de versiones
- Version v1.0.0.0 - Primera version funcional
- Version v1.2.0.0 - Añadida funcionalidad para pasar el texto de la marca de agua en el guion

Instrucciones:
- Se debe pasar como parametro un guion con las opciones de ejecucion del tipo 'clave=valor' con las siguientes opciones:
	* TIPO=1 o 2 (1. Generar codigo QR, 2. Agrega marca de agua)
	* ENTRADA=Fichero PDF al que insertar el QR o la marca de agua
	* SALIDA=Fichero de salida del resultado
	* TEXTOQR=Texto que contendra el codigo QR generado
	* TAMAÑO=Ancho en mm del codigo QR a generar (suponiendo 96 dpi)
	* ALINEACION='derecha' o 'izquierda' - Posicion del codigo QR en la parte superior
	* MARGEN=Margen en mm desde el borde de la pagina
	* MARCA_AGUA=Texto de la marca de agua (para añadir saltos de pagina poner '\n')
	* VERI-FACTU=SI - Indica si añadir al QR los textos oficiales de VERI-FACTU (defecto NO)
	
