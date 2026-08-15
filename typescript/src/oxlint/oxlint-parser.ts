import fs from 'node:fs';

type OxlintDiagnostic={filename?:unknown;severity?:unknown};

export function parseOxlint(file:string){
  const report=JSON.parse(fs.readFileSync(file,'utf8')) as {diagnostics?:OxlintDiagnostic[]};
  const out=new Map<string,{error:number;warning:number;info:number}>();
  for(const diagnostic of report.diagnostics??[]){
    if(typeof diagnostic.filename!=='string')continue;
    const filename=diagnostic.filename.replace(/^file:\/\//,'').replaceAll('\\','/');
    const issues=out.get(filename)??{error:0,warning:0,info:0};
    if(diagnostic.severity==='error')issues.error++;
    else if(diagnostic.severity==='warning')issues.warning++;
    else issues.info++;
    out.set(filename,issues);
  }
  return out;
}
