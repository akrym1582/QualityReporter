import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {analyzeSource} from '../src/analysis/function-metrics.js';

test('function metrics recognizes components hooks methods and arrow functions',()=>{
  const directory=fs.mkdtempSync(path.join(os.tmpdir(),'quality-functions-'));
  const file=path.join(directory,'OrderPage.tsx');
  fs.writeFileSync(file,`function OrderPage({ok}:{ok:boolean}) { if(ok) return <div/>; return null; }
const useOrder = () => { for(let i=0;i<2;i++) {} };
const helper = (a:boolean,b:boolean) => a && b ? 1 : 0;
class Service { run(x:number) { switch(x) { case 1: return 1; default: return 0; } } }`);
  const result=analyzeSource(file);
  assert.ok(result.loc>=4);
  assert.ok(result.complexity>=8);
  assert.ok(result.functions.some(x=>x.name==='OrderPage'&&x.kind==='function-component'&&x.complexity===2));
  assert.ok(result.functions.some(x=>x.name==='useOrder'&&x.kind==='custom-hook'));
  assert.ok(result.functions.some(x=>x.name==='helper'&&x.kind==='function'&&x.complexity===3));
  assert.ok(result.functions.some(x=>x.name==='run'&&x.kind==='method'));
  fs.rmSync(directory,{recursive:true});
});
