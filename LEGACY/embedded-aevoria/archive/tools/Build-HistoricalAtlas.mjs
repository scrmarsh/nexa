// Development-only importer. Runtime loads the validated embedded JSON; no Node dependency.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const source=JSON.parse(fs.readFileSync(path.join(root,'docs/historical-atlas-source.json'),'utf8'));
// Published IDs must survive display-name edits. Add an explicit override/alias when renaming.
const identityOverrides={};
const blocks=source.Blocks, entities=new Map(), eras=[], cultures=new Map(), dynasties=new Map(), representations=new Map(), relationships=[], wars=[], technologies=[], branches=[];
const block=id=>blocks.find(b=>b.Index===id);
const slug=s=>s.normalize('NFKD').replace(/\p{M}/gu,'').toLowerCase().replace(/æ/g,'ae').replace(/ø/g,'o').replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'');
const ref=(name,b,era,classification='ReferenceUnverified')=>({Name:name,BlockId:b.Index,EraId:era,Classification:classification});
const governments=[
    {Id:'monarchy',Name:'Monarchy'},{Id:'republic',Name:'Republic'},{Id:'empire',Name:'Imperial government'},
    {Id:'confederation',Name:'Confederation'},{Id:'unspecified',Name:'Unspecified in reference'}
];
function culture(id,name){if(!cultures.has(id))cultures.set(id,{Id:id,Name:name});return id;}
function representation(name,kind='AtlasArtDirection',sourceBlockId=185){
    const id='symbol:'+slug(name);if(!representations.has(id))representations.set(id,{Id:id,Name:name,Usage:kind,Playable:false,SourceBlockId:sourceBlockId});return id;
}
function add(name,scope,b,era,{alias=null,kind=null,origin=null,classification='ReferenceUnverified',status='HistoricalReference'}={}){
    const display=alias??name;const key=scope+':'+slug(display);const id=identityOverrides[key]??key;
    let e=entities.get(id);
    if(!e){e={Id:id,Name:display,Aliases:[],Kind:kind??(/culture|communities|societies|polities|confederations|peoples/i.test(name) ? 'Aggregate' : /dynast(?:y|ies)/i.test(name) ? 'Dynasty' : 'Unclassified'),Status:status,CultureId:null,DynastyId:null,GovernmentId:null,RulerTitles:[],Architecture:null,RepresentationId:null,Origin:origin,EmergenceYear:null,EndYear:null,EraIds:[],MilitaryCharacteristics:[],Occurrences:[],DiplomaticStatus:null,Notes:[]};entities.set(id,e);}
    if(e.Name!==display)throw Error('Slug collision: '+name);
    if(name!==display && !e.Aliases.includes(name))e.Aliases.push(name);
    if(era && !e.EraIds.includes(era))e.EraIds.push(era);
    if(origin)e.Origin=origin;if(kind)e.Kind=kind;
    e.Occurrences.push(ref(name,b,era,classification));return e;
}

const headings=blocks.filter(b=>b.Kind==='paragraph' && /^\d+\. /.test(b.Cells[0]) && b.Index<43);
const ranges=[[null,-3500],[-3500,-2000],[-2000,-1200],[-1200,-500],[-500,500],[500,1000],[1000,1500],[1500,1700],[1700,1914],[1900,2026]];
const modernNames=new Set();
for(const i of [50,52,54,56,58,60])for(const name of block(i).Cells[0].split('•').map(s=>s.trim()))modernNames.add(name);
function historicalScope(name,eraIndex){
    const stripped=name.replace(/^modern /i,'');
    // An ancient kingdom and a contemporary country can share a name without being one polity.
    const modern=modernNames.has(stripped) && (eraIndex===9 || /^modern /i.test(name) || ['United States','United Kingdom','Canada','Australia','New Zealand'].includes(name));
    return modern ? ['country',stripped] : ['historical',name];
}
for(let i=0;i<headings.length;i++){
    const heading=headings[i],era='historical-'+(i+1);
    eras.push({Id:era,Name:heading.Cells[0].replace(/^\d+\. /,''),StartYear:ranges[i][0],EndYear:ranges[i][1],Fictional:false,Features:block(heading.Index+1).Cells[0],BlockId:heading.Index});
    const names=block(heading.Index+2);for(const name of names.Cells[0].replace(/\.$/,'').split(';').map(s=>s.trim()).filter(Boolean)){
        const [scope,canonical]=historicalScope(name,i);add(name,scope,names,era,{alias:canonical,classification:/culture|communities|societies|polities|confederations/i.test(name) ? 'CulturalUmbrella' : 'ReferenceUnverified'});
    }
}
for(const i of [50,52,54,56,58,60]){
    const row=block(i),region=block(i-1).Cells[0].replace(/ \(\d+\)$/,'');
    for(const name of row.Cells[0].split('•').map(s=>s.trim())){
        const e=add(name,'country',row,'historical-10',{kind:'State',origin:region+' (atlas coverage grouping)'});e.DiplomaticStatus='UN member in reference inventory';
    }
}
for(const name of ['Holy See','Palestine']){const e=add(name,'country',block(61),'historical-10',{kind:'State'});e.DiplomaticStatus='UN non-member observer in reference inventory';}
// These are textual-source associations, never a blanket archaeological assertion.
for(const name of ['Israel','Judah','Canaanites','Amorites','Hittites','Jebusites','Perizzites','Hivites','Girgashites','Midianites','Amalekites','Edomites','Moabites','Ammonites','Philistines']){
    const historical=entities.get('historical:'+slug(name));
    if(historical)historical.Occurrences.push(ref(name,block(44),null,'BiblicalTradition'));
    else add(name,'tradition',block(44),null,{kind:'Aggregate',classification:'BiblicalTradition',status:'TraditionalReference'});
}
add('Genesis primeval figures and pre-Flood peoples','tradition',block(44),null,{kind:'NarrativeTradition',classification:'MythicLegendary',status:'TraditionalReference'});

for(const b of blocks.filter(b=>b.Kind==='row' && b.Index>=67 && b.Index<=75)){
    const [range,name,features]=b.Cells;const [start,end]=range.split('–').map(Number);
    if(!Number.isInteger(start)||!Number.isInteger(end))throw Error('Unrecognized future era row');
    eras.push({Id:'future-'+start,Name:name,StartYear:start,EndYear:end,Fictional:true,Features:features,BlockId:b.Index});
}
for(const b of blocks.filter(b=>b.Kind==='row' && /^\d{4}$/.test(b.Cells[0]))){
    const [year,name,rootSymbol,identity]=b.Cells;const [origin,symbol]=rootSymbol.split('•').map(s=>s.trim());
    const era=eras.find(e=>e.Fictional && +year>=e.StartYear && +year<=e.EndYear);
    if(!era || !symbol)throw Error('Incomplete future polity '+name);
    const e=add(name,'future',b,era.Id,{kind:'State',origin,classification:'FictionalSpeculative',status:'FictionalScenario'});
    e.EmergenceYear=+year;e.RepresentationId=representation(symbol,'AtlasArtDirection',b.Index);e.MilitaryCharacteristics=[identity];
}
for(const b of blocks.filter(b=>b.Kind==='paragraph' && /^\d{4}–\d{4} — /.test(b.Cells[0]))){
    const m=b.Cells[0].match(/^(\d{4})–(\d{4}) — (.+)$/);
    wars.push({Id:'war:'+slug(m[3]),Name:m[3],StartYear:+m[1],EndYear:+m[2],Description:block(b.Index+1).Cells[0],BlockId:b.Index,Fictional:true,Automatic:false});
}
for(const b of blocks.filter(b=>b.Kind==='row' && b.Index>=163 && b.Index<=168)){
    const [period,vehicles,weapons,rule]=b.Cells;const [start,end]=period.split('–').map(Number);
    technologies.push({Id:'technology-'+start,StartYear:start,EndYear:end,Vehicles:vehicles,Weapons:weapons,Rule:rule,BlockId:b.Index,Fictional:true});
}

// Small explicit curation layer: no automatic monarchy/title/date guesses for the inventory.
function curate(id,cultureName,government,titles,kind='State'){
    const e=entities.get(id);if(!e)throw Error('Missing curated identity '+id);
    e.CultureId=culture('culture:'+slug(cultureName),cultureName);e.GovernmentId=government;e.RulerTitles=titles;e.Kind=kind;
}
curate('historical:roman-kingdom','Roman','monarchy',['King'],'StatePhase');
curate('historical:early-roman-republic','Roman','republic',['Consul'],'StatePhase');
curate('historical:roman-republic','Roman','republic',['Consul'],'StatePhase');
curate('historical:roman-empire','Roman','empire',['Emperor'],'StatePhase');
curate('historical:western-roman-empire','Roman','empire',['Emperor'],'StatePhase');
curate('historical:eastern-roman-empire','Roman','empire',['Emperor'],'StatePhase');
curate('country:united-states','American','republic',['President']);
curate('historical:mongol-empire','Mongol','empire',['Khan']);
curate('historical:england','English','monarchy',['King','Queen']);
curate('historical:scotland','Scottish','monarchy',['King','Queen']);
// Scenario culture vocabulary requested for Phase 3. These are broad design links,
// not new political entities, dated chronologies or claims about reference governments.
for(const [id,name] of [['greek','Greek'],['french','French'],['japanese','Japanese'],['egyptian','Egyptian']])
    culture('culture:'+id,name+' (broad scenario cultural reference)');
entities.get('country:united-states').RepresentationId=representation('Bald Eagle','GameplayAvatar');
entities.get('country:united-kingdom').RepresentationId=representation('Lion','GameplayAvatar');
const troy=entities.get('historical:wilusa-troy');troy.Kind='Settlement';
troy.Occurrences=troy.Occurrences.map(o=>({...o,Classification:'Debated'}));
troy.Notes.push('The atlas distinguishes an archaeological city from legendary Trojan War narratives; Wilusa/Troy identification and chronology need specialist review.');
for(const name of ['Ming','Qing','Qin dynasty','Western and Eastern Han','Tang','Sui']){
    const e=entities.get('historical:'+slug(name));if(!e)throw Error('Missing dynastic reference '+name);
    const id='dynasty:'+slug(name);e.CultureId=culture('culture:chinese','Chinese (broad cultural reference)');e.DynastyId=id;
    dynasties.set(id,{Id:id,Name:name,CultureId:e.CultureId});
}
function relation(from,to,kind,provenance){
    if(!entities.has(from)||!entities.has(to))throw Error('Unresolved relationship '+from+' -> '+to);
    relationships.push({Id:'relationship-'+(relationships.length+1),FromId:from,ToId:to,Kind:kind,Provenance:provenance,Forced:false});
}
relation('historical:roman-kingdom','historical:early-roman-republic','PoliticalContinuity','User specification: Roman kingdom/republic/empire phases; no transition date asserted.');
relation('historical:early-roman-republic','historical:roman-republic','PoliticalContinuity','Same Roman political/cultural thread; overlapping inventory labels retained.');
relation('historical:roman-republic','historical:roman-empire','PoliticalContinuity','User specification: Roman kingdom/republic/empire phases; no mandatory simulation outcome.');
for(const name of ['Wessex','Mercia','Northumbria','East Anglia','Essex','Sussex','Kent'])relation('historical:'+slug(name),'historical:england','ContributedTo','User specification: Anglo-Saxon contributions to England; not one compulsory merger event.');
for(const name of ['England','Scotland'])relation('historical:'+slug(name),'historical:great-britain','UnionComponent','User specification: political union; English and Scottish cultures remain distinct.');
relation('historical:great-britain','country:united-kingdom','PoliticalContinuity','User specification: British institutional continuity, not an assertion of unchanged territory or government.');

const resolveFuture=name=>{
    const aliases={'Celadon Matrix':'Zhong-Xi Celadon Matrix','Khepra-Nil Sun Court':'The Khepra-Nil Sun Court','Sahel Greenbelt':'Sahel Greenbelt Union','Federated Sunbelt':'Federated Sunbelt of Africa'};
    const value=aliases[name]??name;const e=entities.get('future:'+slug(value));if(!e)throw Error('Unresolved future branch '+name);
    if(name!==value && !e.Aliases.includes(name))e.Aliases.push(name);return e.Id;
};
for(const b of blocks.filter(b=>b.Kind==='row' && b.Index>=171 && b.Index<=178)){
    const [thread,near,far]=b.Cells;
    const names=near.includes('→') ? near.split('→').map(s=>s.trim()) : near==='Nile-descended future solar settlements' ? [] : [near];
    const ids=names.map(resolveFuture);ids.push(resolveFuture(far));
    branches.push({Id:'branch-'+b.Index,Thread:thread,NearText:near,FarText:far,EntityIds:ids,BlockId:b.Index,Fictional:true});
    for(let i=1;i<ids.length;i++)relation(ids[i-1],ids[i],'FictionalBranch','Atlas block '+b.Index+': possible branch, not a guaranteed transition.');
}
const catalog={SchemaVersion:1,SourceDocument:source.Document,SourceSha256:source.Sha256,ProposedVersion:'0.2.0 Alpha',Entities:[...entities.values()],Cultures:[...cultures.values()],Dynasties:[...dynasties.values()],Governments:governments,Representations:[...representations.values()],Eras:eras,Relationships:relationships,Wars:wars,Technologies:technologies,Branches:branches};
if(headings.length!==10 || catalog.Entities.filter(e=>e.Status==='FictionalScenario').length!==55 || wars.length!==13 || eras.filter(e=>e.Fictional).length!==9 || technologies.length!==6 || branches.length!==8)throw Error('Incomplete reference import');
if(catalog.Entities.filter(e=>e.DiplomaticStatus==='UN member in reference inventory').length!==193)throw Error('Contemporary reference coverage mismatch');
fs.mkdirSync(path.join(root,'assets/history'),{recursive:true});
const target=path.join(root,'assets/history/civilizations.json');
if(fs.existsSync(target))for(const previous of JSON.parse(fs.readFileSync(target,'utf8')).Entities)
    if(!entities.has(previous.Id))throw Error('Existing identity would be removed: '+previous.Id+'. Add an explicit identity override/alias or reviewed migration.');
fs.writeFileSync(target,JSON.stringify(catalog,null,2)+'\n');
console.log(`Imported ${catalog.Entities.length} identities, ${eras.length} eras, 55 fictional polities, ${wars.length} fictional conflicts; historical dates unspecified where absent from the atlas.`);
