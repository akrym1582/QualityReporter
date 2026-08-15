import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import {parseCoverage} from '../src/coverage/coverage-parser.js';
import {parseEslint} from '../src/eslint/eslint-parser.js';
import {parseOxlint} from '../src/oxlint/oxlint-parser.js';

function fixture(name:string, value:unknown):string {
  const directory=fs.mkdtempSync(path.join(os.tmpdir(),'quality-ts-'));
  const file=path.join(directory,name);
  fs.writeFileSync(file,JSON.stringify(value));
  return file;
}

test('coverage parser calculates statement and branch percentages',()=>{
  const file=fixture('coverage.json',{'/repo/a.ts':{s:{0:1,1:0},b:{0:[1,0],1:[2,3]}}});
  const result=parseCoverage(file).get('/repo/a.ts');
  assert.deepEqual(result,{lineCoverage:50,branchCoverage:75});
  fs.rmSync(path.dirname(file),{recursive:true});
});

test('eslint parser aggregates error warning and informational messages',()=>{
  const file=fixture('eslint.json',[{filePath:'C:\\repo\\a.ts',messages:[{severity:2},{severity:1},{severity:0}]}]);
  assert.deepEqual(parseEslint(file).get('C:/repo/a.ts'),{error:1,warning:1,info:1});
  fs.rmSync(path.dirname(file),{recursive:true});
});


test('oxlint parser aggregates diagnostics by filename and severity',()=>{
  const file=fixture('oxlint.json',{diagnostics:[
    {filename:'file:///repo/a.ts',severity:'error'},
    {filename:'file:///repo/a.ts',severity:'warning'},
    {filename:'file:///repo/a.ts',severity:'advice'},
    {filename:'C:\\repo\\b.ts',severity:'warning'},
  ]});
  const result=parseOxlint(file);
  assert.deepEqual(result.get('/repo/a.ts'),{error:1,warning:1,info:1});
  assert.deepEqual(result.get('C:/repo/b.ts'),{error:0,warning:1,info:0});
  fs.rmSync(path.dirname(file),{recursive:true});
});
