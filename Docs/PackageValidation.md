# Package Validation - Sector 7

Regra usada pelo jogo:

- Se todos os campos da caixa batem com o relatorio, o botao correto e `ACCEPT`.
- Se qualquer campo difere, o botao correto e `REJECT`.
- Campos validados: shape, barcode, logo, tapeColor, destination, weight.
- O `GameManager` considera correto quando `accepted != packageData.ShouldReject`.

O tempo da caixa e exibido no painel frontal como `CAIXA XXs`.
Os erros sao exibidos no painel frontal como `ERROS 0/3`.

| ID | Dificuldade | Diferencas entre caixa e relatorio | Botao correto esperado | Configurado no jogo | Estado |
| --- | --- | --- | --- | --- | --- |
| R001 | easy | none | ACCEPT | ACCEPT | OK |
| R002 | easy | none | ACCEPT | ACCEPT | OK |
| R003 | easy | none | ACCEPT | ACCEPT | OK |
| R004 | easy | none | ACCEPT | ACCEPT | OK |
| R005 | easy | none | ACCEPT | ACCEPT | OK |
| R006 | easy | barcode | REJECT | REJECT | OK |
| R007 | easy | tapeColor | REJECT | REJECT | OK |
| R008 | easy | shape | REJECT | REJECT | OK |
| R009 | easy | destination | REJECT | REJECT | OK |
| R010 | easy | weight | REJECT | REJECT | OK |
| R011 | medium | none | ACCEPT | ACCEPT | OK |
| R012 | medium | none | ACCEPT | ACCEPT | OK |
| R013 | medium | none | ACCEPT | ACCEPT | OK |
| R014 | medium | none | ACCEPT | ACCEPT | OK |
| R015 | medium | none | ACCEPT | ACCEPT | OK |
| R016 | medium | barcode | REJECT | REJECT | OK |
| R017 | medium | logo | REJECT | REJECT | OK |
| R018 | medium | destination | REJECT | REJECT | OK |
| R019 | medium | weight | REJECT | REJECT | OK |
| R020 | medium | shape, tapeColor | REJECT | REJECT | OK |
| R021 | hard | tapeColor | REJECT | REJECT | OK |
| R022 | hard | none | ACCEPT | ACCEPT | OK |
| R023 | hard | none | ACCEPT | ACCEPT | OK |
| R024 | hard | weight | REJECT | REJECT | OK |
| R025 | hard | none | ACCEPT | ACCEPT | OK |
| R026 | hard | shape | REJECT | REJECT | OK |
| R027 | hard | barcode | REJECT | REJECT | OK |
| R028 | hard | logo | REJECT | REJECT | OK |
| R029 | hard | tapeColor | REJECT | REJECT | OK |
| R030 | hard | none | ACCEPT | ACCEPT | OK |
