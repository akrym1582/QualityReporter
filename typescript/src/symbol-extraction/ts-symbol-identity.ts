import {createHash} from 'node:crypto';
export function buildSymbolKey(modulePath:string,containingClass:string|undefined,name:string,parameterCount:number){return `${modulePath.replaceAll('\\','/')}::${containingClass??''}::${name}::${parameterCount}`;}
export function buildSymbolId(key:string){return createHash('sha256').update(key).digest('hex');}
