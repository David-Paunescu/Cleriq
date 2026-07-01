export function normalizeazaPentruCautare(text: string): string {
  return text
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');
}

// Avertisment GDPR partajat: publicarea unei dispozi\u021bii INDIVIDUALE (act de personal) e un override
// deliberat \u2014 o singur\u0103 surs\u0103, folosit\u0103 de dialogul de publicare pe portal + blocul individual din
// publicare-mol-dialog. Oglinde\u0219te inten\u021bia g\u0103rzii backend GateazaPublicareIndividuala.
export const AVERTISMENT_PUBLICARE_INDIVIDUALA =
  'Dispozi\u021bia are caracter individual (act de personal) \u0219i poate con\u021bine date cu caracter personal. ' +
  'Nu se public\u0103 implicit \u2014 anonimizarea datelor personale r\u0103m\u00e2ne sarcina ta. ' +
  'Confirm\u0103 cu un motiv pentru a continua.';